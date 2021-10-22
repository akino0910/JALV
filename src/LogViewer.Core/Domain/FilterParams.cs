using System;

namespace LogViewer.Core.Domain
{
    public class FilterParams
    {
        public DateTime? Date { get; set; }

        public int Level { get; set; }

        public string Thread { get; set; }

        public string Logger { get; set; }

        public string Message { get; set; }

        public string Pattern { get; set; }

        public override string ToString()
        {
            return $"Date: {Date}, Level: {Level}, Thread: {Thread}, Logger: {Logger}, Message: {Message}";
        }
    }
}