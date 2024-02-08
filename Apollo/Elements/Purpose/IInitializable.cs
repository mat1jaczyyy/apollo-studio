using Apollo.Enums;

namespace Apollo.Elements.Purpose {
    public interface IInitializable {
        PurposeType Purpose { get; }
        bool Disposed { get; }

        void Initialized();

        void Initialize() {
            new PurposeController(this);
        }
    }
}
