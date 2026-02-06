using UnityEngine;

namespace XRMultiplayer
{
    public abstract class AudioClipLoader : MonoBehaviour
    {
        protected const string ANNOUNCER_CLIP_FOLDER = "Announcers";

        protected const string START_SUFFIX = "Start";

        protected const string LOADED_SUFFIX = "Loaded";

        protected const string CONNECTION_FAILED = "ConnectionFailed";

        protected abstract AudioClip[] LoadAllClipFormResources(string relativePath);
    }
}