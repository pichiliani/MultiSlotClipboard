// Guids.cs
// MUST match guids.h
using System;

namespace Company.MultiSlotClipboard
{
    static class GuidList
    {
        public const string guidMultiSlotClipboardPkgString = "d8718fdd-8732-48ae-be80-63c7ac78e905";
        public const string guidMultiSlotClipboardCmdSetString = "5641152b-3b25-49ac-81c5-c305a466047c";
        public const string guidToolWindowPersistanceString = "0c43e10b-16d9-4404-a4ca-689eefc73fd1";

        public static readonly Guid guidMultiSlotClipboardCmdSet = new Guid(guidMultiSlotClipboardCmdSetString);
    };
}