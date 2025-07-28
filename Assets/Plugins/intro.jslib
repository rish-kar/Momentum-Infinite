mergeInto(LibraryManager.library, {
  PlayIntroVideo: function() {
    var video = document.createElement('video');
    video.src = 'StreamingAssets/MomentumIntro.mp4';
    video.autoplay = true;
    video.style.position = 'absolute';
    video.style.top = '0';
    video.style.left = '0';
    video.style.width = '100%';
    video.style.height = '100%';
    document.body.appendChild(video);
    video.onended = function() {
      video.remove();
      // tell Unity to load the next scene
      unityInstance.SendMessage('YourVideoPlayerGameObjectName', 'OnVideoFinished');
    };
  }
});
