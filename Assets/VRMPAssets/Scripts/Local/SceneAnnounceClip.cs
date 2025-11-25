using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SceneAnnounceClip
{
    [SerializeField] private string folderHead;
    [SerializeField] private List<AudioClip> loadStartClips;

    [SerializeField] private List<AudioClip> loadedClips;
    [SerializeField] private AudioClip loadFailedClip;

    private const string ANNOUNCER_CLIP_FOLDER = "Announcers";
    private const string START_SUFFIX = "Start";
    private const string LOADED_SUFFIX = "Loaded";
    private const string LOAD_FAILED_SUFFIX = "LoadFailed";

    public void LoadClips()
    {
        var startClipsPath = $"{folderHead}/{START_SUFFIX}";
        loadStartClips.AddRange(this.LoadAllClipFormResources(startClipsPath));

        var loadedClipsPath = $"{folderHead}/{LOADED_SUFFIX}";
        loadedClips.AddRange(this.LoadAllClipFormResources(loadedClipsPath));

        var loadFailedClipPath = $"{LOAD_FAILED_SUFFIX}";
        var faildClips = this.LoadAllClipFormResources(loadFailedClipPath);
        if (faildClips != null)
        {
            this.loadFailedClip = faildClips.FirstOrDefault();
        }
    }

    public AudioClip LoadFailedClip => loadFailedClip;

    public AudioClip GetLoadStartClipRandom()
    {
        return this.loadStartClips[Random.Range(0, this.loadStartClips.Count)];
    }

    public AudioClip GetLoadedClipRandom()
    {
        return this.loadedClips[Random.Range(0, this.loadedClips.Count)];
    }

    private AudioClip[] LoadAllClipFormResources(string relativePath)
    {
        var loadPath = $"{ANNOUNCER_CLIP_FOLDER}/{relativePath}";
        var audioClip = Resources.LoadAll<AudioClip>(loadPath);
        if (audioClip != null)
        {
            return audioClip;
        }
        else
        {
            Debug.LogWarning($"[SceneAnnouncerController] Announcer clip not found at path: {loadPath}");
            return null;
        }
    }
}