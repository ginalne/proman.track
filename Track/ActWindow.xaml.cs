
using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Track
{
    public partial class ActWindow : INotifyPropertyChanged
    {
        private int onPage = 1;
        private TaskActivity activity = null;
        private DateTime trackDate;
        private uint trackDuration = 0;
        private ObservableCollection<TaskActivity> _activities;
        private bool isMoreable = true;
        public bool onPlay = false;
        static public ActWindow singleton;
        private readonly BackgroundWorker worker, searchWorker;
        private int sleepRate;
        private uint stepSearch = 0;
        private string hintSearch = "";

        public ActWindow() {
            DataContext = singleton = this;
            InitializeComponent();
            onPropertyChanged("acts_name");
            _activities = new ObservableCollection<TaskActivity> { };
            getState();
            getActivities(1);
            worker = new BackgroundWorker();
            searchWorker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            searchWorker.DoWork += worker_Search;
            onPropertyChanged("Activities");
            headerSearch = new ObservableCollection<TaskHeader>();
            onPropertyChanged("headerSearch");
            onPropertyChanged("onSearch");
        }
        public IList<TaskActivity> Activities {
            get { return _activities; }
        }
        public ObservableCollection<TaskHeader> headerSearch { get; set; }
        public bool onSearch {
            get {
                return headerSearch == null || headerSearch.Count > 0;
            }
        }

        public TaskHeader actsSelected {
            set {
                if (value != null) {
                    Trace.WriteLine(value.Name);
                    acts = new TaskActivity {
                        header = value,
                        task = value.task
                    };
                    headerSearch.Clear();
                    onPropertyChanged("onSearch");
                }
            }
        }

        public async void getState() {
            var response = await HttpService.i.postSimple<UserState>("state", new Dictionary<string, string> { });
            if (response == null) {
                MessageBox.Show("HTTP Request Error.");
            } else {
                var data = response.data;
                if (data.on_track && data.start != null) {
                    trackDate = (DateTime)data.start;
                    activity = new TaskActivity {
                        id = 0,
                        make = 0,
                        date = trackDate.Date,
                        duration = 0,
                        header = data.header,
                        task = data.header.task,
                        time = new ObservableCollection<TaskActivityTime>()
                    };
                    isPlaying = true;
                    onPropertyChanged("acts_name");
                    onPropertyChanged("acts_path");
                    worker.RunWorkerAsync(false);
                }
            }
        }
        public async void getActivities(int page) {
            var res = await HttpService.i.postSimple<TaskActivity[]>("act/list", new Dictionary<string, string> { { "page", page.ToString() } });
            foreach (TaskActivity _task in res.data) {
                _activities.Add(_task);
            }
            onPage = int.Parse(res.message);
        }

        public bool isPlaying {
            get { return onPlay; }
            set {
                onPlay = value;
                App.i.setTrackState(value);
                onPropertyChanged("acts_button");
                onPropertyChanged("acts_button_hover");
                onPropertyChanged("isPlaying");
                onPropertyChanged("isStoping");
            }
        }
        public bool isStoping {
            get { return !onPlay; }
            set {
                onPlay = !value;
                onPropertyChanged("acts_button");
                onPropertyChanged("acts_button_hover");
                onPropertyChanged("isPlaying");
                onPropertyChanged("isStoping");
            }
        }


        public string acts_path {
            get { return activity == null ? "-" : activity.Path; }
        }
        public string acts_name {
            get { return activity == null ? "" : activity.Name; }
        }
        async void worker_Search(object sender, DoWorkEventArgs e) {
            uint lastStep = (uint)e.Argument;
            do {
                lastStep = stepSearch;
                System.Threading.Thread.Sleep(750);
                Trace.WriteLine("try search on " + stepSearch + " vs " + lastStep);
            } while (lastStep != stepSearch);
            APIResponse<TaskHeader[]> response = await HttpService.i.postSimple<TaskHeader[]>(
                    "search/header",
                    new Dictionary<string, string> { { "hint", hintSearch } }
                );
            if (response == null)
                return;
            App.Current.Dispatcher.Invoke((Action)delegate { // <--- HERE
                if (headerSearch.Count > 0)
                    headerSearch.Clear();
                foreach (TaskHeader header in response.data) {
                    headerSearch.Add(header);
                }
            }
            );
            onPropertyChanged("onSearch");
        }
        public string acts_start {
            get { return onPlay ? trackDate.ToString("dd MMMM yyyy, HH:mm:ss") : "-"; }
        }
        public string acts_duration {
            get {
                return onPlay ? trackDuration >= 3600 ?
                  (trackDuration / 3600).ToString() + "h " + (trackDuration / 60 % 60).ToString() + "m" :
                  (trackDuration / 60).ToString() + "m " + (trackDuration % 60).ToString() + "s" : "-";
            }
        }
        public string acts_button {
            get { return onPlay ? "#a55" : (activity == null ? "#aaa" : "#5a5"); }
        }
        public string acts_button_hover {
            get { return onPlay ? "#944" : (activity == null ? "#999" : "#494"); }
        }
        void actsInput(object sender, RoutedEventArgs e) {
            TextBox textBox = sender as TextBox;
            if (activity != null) {
                if (textBox.Text != activity.Name) {
                    acts = null;
                }
            } else {
                stepSearch++;
                Trace.WriteLine("Try to search " + textBox.Text + "(" + stepSearch + ")");
                hintSearch = textBox.Text;
                if (!searchWorker.IsBusy)
                    searchWorker.RunWorkerAsync(stepSearch);
            }
        }
        public TaskActivity acts {
            get { return activity; }
            set {
                bool changed = isStoping && ((value == null && activity != null) || (value != null && activity != value));
                if (changed) {
                    activity = value;
                    onPropertyChanged("acts_path");
                    onPropertyChanged("acts_name");
                    onPropertyChanged("acts_start");
                    onPropertyChanged("acts_duration");
                    onPropertyChanged("acts_button");
                    onPropertyChanged("acts_button_hover");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void toggleTimer(object sender, RoutedEventArgs e) {
            if (isPlaying) {
                finishActivity();
                Trace.WriteLine("Stop Track Activities");
            } else
            if (activity != null) {
                if (!worker.IsBusy) {
                    isPlaying = true;
                    trackDate = DateTime.Now;
                    worker.RunWorkerAsync(true);
                }
            }
        }

        public void getMore(object sender, RoutedEventArgs e) {
            if (isMoreable) {
                getActivities(onPage);
            }
        }

        private void onPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosing(CancelEventArgs e) {
            Hide();
            //Visibility = Visibility.Hidden;
            e.Cancel = true;
        }
        async void worker_DoWork(object sender, DoWorkEventArgs e) {
            Trace.WriteLine("Start Activity Worker : " + activity.Name);
            sleepRate = 1000;
            trackDuration = (uint)(DateTime.Now - trackDate).TotalSeconds;
            onPropertyChanged("acts_start");
            onPropertyChanged("acts_duration");
            if ((bool)e.Argument) {
                APIResponse<bool> response = await HttpService.i.postSimple<bool>(
                        "track:start",
                        new Dictionary<string, string> { { "header_id", activity.header.id.ToString() }, { "start", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") } }
                    );
                if (response == null || response.message == "NOT FOUND") {
                    MessageBox.Show(response == null ? "HTTP Request Error." : "Sorry, Unfortunatly Task Header is not found!");
                    finishActivity();
                    return;
                }
            }
            if (isPlaying)
                System.Threading.Thread.Sleep(sleepRate);
            while (isPlaying) {
                var idleTime = IdleTimeDetector.GetIdleTimeInfo();
                if (idleTime.IdleTime.TotalMinutes >= 5) {
                    MessageBoxResult answer = MessageBox.Show("The System detects that you have been idle for more than 5 minutes, therefore we stop tracking your activity from the last time you were active.", "Idle Detection", MessageBoxButton.OKCancel);
                    if (answer == MessageBoxResult.OK) {
                        trackDuration += (uint)((sleepRate / 1000) - idleTime.IdleTime.TotalSeconds);
                        finishActivity();
                        return;
                    }
                }
                trackDuration += (uint)(sleepRate / 1000);
                onPropertyChanged("acts_duration");
                if (trackDuration >= 3600) {
                    sleepRate = 60000;
                }
                System.Threading.Thread.Sleep(sleepRate);
            }
        }

        async void finishActivity() {
            if (isPlaying && activity != null) {
                //TO DO Send to Server for stop & save current activity tracking
                isPlaying = false;
                APIResponse<bool> response = await HttpService.i.postSimple<bool>(
                        "track:stop",
                        new Dictionary<string, string> { { "header_id", activity.header.id.ToString() }, { "stoptime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") } }
                    );
                if (response == null) {
                    MessageBox.Show("HTTP Request Error.");
                    return;
                }

                if (trackDuration > 10) {
                    TaskActivity target = null;
                    DateTime onDate = trackDate.Date;
                    uint dayDuration;
                    while (trackDuration > 0) {
                        dayDuration = Math.Min(((TimeOnly)trackDate).restDay, trackDuration);
                        foreach (TaskActivity task in _activities) {
                            Trace.WriteLine(task.Name + " " + task.date + " vs " + onDate);
                            if (task.date == onDate) {
                                if (task.task.id == activity.task.id) {
                                    target = task;
                                    break;
                                }
                            } else
                            if (task.date < onDate)
                                break;
                        }
                        if (target == null)
                            _activities.Insert(0, target = new TaskActivity(activity.make, activity.Weight, 0, activity.Name) {
                                id = activity.id,
                                date = onDate,
                                header = activity.header,
                                task = activity.task
                            });
                        target.addDuration(trackDuration);
                        target.time.Add(new TaskActivityTime() { start = (TimeOnly)trackDate, end = (TimeOnly)trackDate.AddSeconds(dayDuration) });
                        trackDuration -= dayDuration;
                        trackDate = onDate =  onDate.AddDays(1);
                        target = null;
                    }
                }
                onPropertyChanged("Activities");
                onPropertyChanged("acts_duration");
                onPropertyChanged("acts_start");
                onPropertyChanged("acts_button");
                onPropertyChanged("acts_button_hover");
                Trace.WriteLine("End Activity : " + activity.Name + " for " + trackDuration + " seconds");
            }
        }
    }
}
