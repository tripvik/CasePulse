using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Platforms.Android
{
    public static class AndroidServiceBridge
    {
        public static IConnectionService? BluetoothService { get; set; }
        public static ITranscriptionService? TranscriptionService { get; set; }
        public static Action<string>? OnDisconnected { get; set; }
        public static Action<TranscriptEntry>? OnFinalTranscript;
        public static Action<TranscriptEntry>? OnRecognizingTranscript;

    }
}
