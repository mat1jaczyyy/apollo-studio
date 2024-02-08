using System;

using Apollo.Core;
using Apollo.Enums;

namespace Apollo.Elements.Purpose {
    public class PurposeController {
        IInitializable For;
        bool ListeningToProjectLoaded;

        public PurposeController(IInitializable element) {
            For = element;
            Initialize();
        }

        void Initialize() {
            if (For.Purpose == PurposeType.Unknown)
                throw new Exception("Purpose is unknown while initializing!");

            if (For.Purpose == PurposeType.Passive)
                return;

            if (!For.Disposed) {
                if (For.Purpose == PurposeType.Unrelated) {
                    For.Initialized();
                    return;
                }

                // Purpose is always Active at this point

                if (Program.Project == null) {
                    Program.ProjectLoaded += Initialize;
                    ListeningToProjectLoaded = true;
                    return;
                }

                For.Initialized();
            }

            if (ListeningToProjectLoaded) {
                Program.ProjectLoaded -= Initialize;
                ListeningToProjectLoaded = false;
            }
        }
    }
}
