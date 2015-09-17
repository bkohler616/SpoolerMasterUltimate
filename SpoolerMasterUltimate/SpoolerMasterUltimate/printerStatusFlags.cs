namespace SpoolerMasterUltimate {
    public static class PrinterStatusFlagInfo {
        public static class PrintJobStatusFlags {
            public static uint Paused = 0x1;
            public static uint Error = 0x2;
            public static uint Deleting = 0x4;
            public static uint Spooling = 0x8;
            public static uint Printing = 0x10;
            public static uint Offline = 0x20;
            public static uint Paperout = 0x40;
            public static uint Printed = 0x80;
            public static uint Deleted = 0x100;
            public static uint Blocked_DevQ = 0x200;
            public static uint User_Intervention_Req = 0x400;
            public static uint Restart = 0x800;
        }

        public static class PrinterStatusFlags {
            public static uint Other = 0x1;
            public static uint Unknown = 0x2;
            public static uint Idle = 0x3;
            public static uint Printing = 0x4;
            public static uint WarmingUp = 0x5;
            public static uint StoppedPrinting = 0x6;
            public static uint Offline = 0x7;
            public static uint Paused = 0x8;
            public static uint Error = 0x9;
            public static uint Busy = 0xA;
            public static uint NotAvailable = 0xB;
            public static uint Waiting = 0xC;
            public static uint Processing = 0xD;
            public static uint Initalization = 0xE;
            public static uint PowerSave = 15;
            public static uint PendingDeletion = 0x10;
            public static uint IOActive = 0x11;
            public static uint ManualFeed = 0x12;

        }
    }
}