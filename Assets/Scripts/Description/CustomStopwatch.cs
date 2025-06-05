using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;

// Originally on DLS.SaveSystem, now on DLS.Description because of you bad C# does not allow ProjectDescription to use classes from outside its namespace
namespace DLS.Description {
    public class CustomStopwatch {
        [JsonIgnore]
        public Stopwatch Stopwatch;
        public TimeSpan StartFrom;
        [JsonIgnore]
        public TimeSpan Elapsed {get => StartFrom.Add(Stopwatch.Elapsed);}
        [JsonConstructor]
        public CustomStopwatch(TimeSpan StartFrom) {
            Stopwatch = Stopwatch.StartNew();
            this.StartFrom = StartFrom;
        }
        [OnSerializing]
        public void Save(StreamingContext unused) => StartFrom = Elapsed;
        public void Save() => StartFrom = Elapsed;
        public CustomStopwatch() {
            Stopwatch = Stopwatch.StartNew();
            StartFrom = new();
        }

    }
}