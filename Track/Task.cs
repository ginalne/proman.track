using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Track
{
    [JsonConverter(typeof(TimeOnlyConverter))]
    public struct TimeOnly
    {
        public string value { get; set; }
        public int sec { get; set; }
        public byte h { get { return (byte)(sec / 3600); } }
        public byte m { get { return (byte)(sec / 60 % 60); } }
        public byte s { get { return (byte)(sec % 60); } }
        public uint restDay {
            get {
                return (uint)(86399 - sec);
            }
        }
        public TimeOnly(int hour, int min, int sec) {
            value = hour.ToString("00") + ":" + min.ToString("00") + ":" + sec.ToString("00");
            this.sec = hour * 3600 + min * 60 + sec;
        }
        public static explicit operator TimeOnly(DateTime dt) {
            return new TimeOnly((byte)dt.Hour, (byte)dt.Minute, (byte)dt.Second);
        }
    }
    public class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            string v = reader.GetString();
            return new TimeOnly { value = v, sec = byte.Parse(v[0..2]) * byte.Parse(v[3..5]) * byte.Parse(v[6..8]) };
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }
    public class Task
    {
        public uint id { get; set; }
        public float weight { get; set; }
        //public TaskActivity[] activity;
        //public TaskVersion[] version;
    }
    public class TaskHeader
    {
        public uint id { get; set; }
        public string name { get; set; }
        public string color { get; set; }
        public string symbol { get; set; }
        public string info { get; set; }
        public TaskHeader[] root { get; set; }
        public Task task { get; set; }
        public TaskHeader() { }

        public TaskHeader(string name = "New Activity") {
            this.name = name;
            color = "ff000";
            symbol = "%";
            info = "New Activity is a new activities.";
        }
        public string Color {
            get { return "#" + color; }
        }
        public string Name {
            get { return symbol + " " + name; }
        }
        public string Path {
            get {
                string path = "";
                foreach (TaskHeader header in root) {
                    path += header.name + "/ ";
                }
                return path;
            }
        }
    }
    public class TaskActivity : INotifyPropertyChanged
    {
        private static uint AutoIncrement = 1;
        public uint id { get; set; }
        public float make { get; set; }
        public DateTime date { get; set; }
        //public float progress;
        public uint duration { get; set; }
        public TaskHeader header { get; set; }
        public Task task { get; set; }
        public ObservableCollection<TaskActivityTime> time { get; set; }
        public TaskActivity() { }
        public TaskActivity(float make, float weight, uint duration, string name = "My New Activities") {
            id = AutoIncrement;
            AutoIncrement++;
            header = new TaskHeader(name);
            task = new Task { weight = weight };
            header.root = new TaskHeader[2] {
                new TaskHeader(),
                new TaskHeader()
            };
            date = DateTime.Now;
            this.make = make;
            //progress = make + 1;
            this.duration = duration;
            time = new ObservableCollection<TaskActivityTime>();
        }

        public string Color {
            get { return "#" + header.color; }
        }
        public float Weight {
            get {
                return task.weight;
            }
        }
        public string Path {
            get {
                if (header == null)
                    return "-";
                return header.Path;
            }
        }
        public string Name {
            get { return header.Name; }
        }
        public string Start {
            get { return date.ToString("dd MMMM yyyy"); }
        }
        public string Duration {
            get {
                return duration >= 3600 ?
                    (duration / 3600).ToString() + "h " + (duration / 60 % 60).ToString() + "m" :
                    (duration / 60).ToString() + "m " + (duration % 60).ToString() + "s";
            }
        }
        public void addDuration(uint increment) {
            duration += increment;
            onPropertyChanged("Duration");
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void onPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class TaskVersion
    {
        public uint id;
        public float weight;
        public DateTime date;
    }
    public class TaskActivityTime
    {
        public TimeOnly start { get; set; }
        public TimeOnly end { get; set; }
        public string TimeSpan {
            get {
                return start.value + " - " + end.value;
            }
        }
    }

    public class UserState
    {
        public uint id { get; set; }
        public uint user_id { get; set; }
        public TaskHeader? header { get; set; }
        public bool on_track { get; set; }
        public DateTime? start { get; set; }
    }
}
