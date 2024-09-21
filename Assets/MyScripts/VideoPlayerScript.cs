using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerScript : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    void Start()
    {
        // Path to the video in the StreamingAssets folder
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "RicePicking_1.mp4");

        // Set the VideoPlayer's URL to the video file
        videoPlayer.url = videoPath;

        // Optionally, configure video player settings like looping, audio output, etc.
        videoPlayer.isLooping = true;

        // Play the video
        videoPlayer.Play();
    }
}
