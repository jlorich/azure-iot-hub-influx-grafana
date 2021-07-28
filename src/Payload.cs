using System;

namespace AzureIotInflux {

    public class Payload {
        public Sensor machine { get; set; }
        public Sensor ambient { get; set; }
        public DateTime timeCreated { get; set; }

        public string deviceId { get; set;}
   
    }
}