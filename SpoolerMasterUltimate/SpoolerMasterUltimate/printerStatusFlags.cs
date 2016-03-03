namespace SpoolerMasterUltimate
{
    public static class PrinterStatusFlagInfo
    {
        public static class PrintJobStatusFlags
        {
            public const uint Paused = 0x1;
            public const uint Error = 0x2;
            public const uint Deleting = 0x4;
            public const uint Spooling = 0x8;
            public const uint Printing = 0x10;
            public const uint Offline = 0x20;
            public const uint Paperout = 0x40;
            public const uint Printed = 0x80;
            public const uint Deleted = 0x100;
            public const uint BlockedDevQ = 0x200;
            public const uint UserInterventionReq = 0x400;
            public const uint Restart = 0x800;
            public const uint AutoPause = 0x1000;
            public const uint AutoDelete = 0x4000;

        }

        public static class PrinterStatusFlags
        {
            public const uint Other = 0x1;
            public const uint Unknown = 0x2;
            public const uint Idle = 0x3;
            public const uint Printing = 0x4;
            public const uint WarmingUp = 0x5;
            public const uint StoppedPrinting = 0x6;
            public const uint Offline = 0x7;
            public const uint Paused = 0x8;
            public const uint Error = 0x9;
            public const uint Busy = 0xA;
            public const uint NotAvailable = 0xB;
            public const uint Waiting = 0xC;
            public const uint Processing = 0xD;
            public const uint Initalization = 0xE;
            public const uint PowerSave = 15;
            public const uint PendingDeletion = 0x10;
            public const uint IoActive = 0x11;
            public const uint ManualFeed = 0x12;
        }
    }
}