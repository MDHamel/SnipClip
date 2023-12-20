using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.StartScreen;

namespace SnipClip
{
	internal class VideoEditor
	{

		public StorageFile RawVideo { get; set; }

		public MediaClip Clip { get; set; }

		public string rawVideoFilePath;
		public string editedVideoFilePath;

		public VideoEditor() {}

		public async Task TrimClipAsync(double start, double end)
		{	
			// Set the start and end points for cropping (adjust as needed)
			TimeSpan st = TimeSpan.FromSeconds(start); // Replace with your desired start time
			TimeSpan et = TimeSpan.FromSeconds(end);   // Replace with your desired end time

			// Trim the clip to the specified time range
			Clip.TrimTimeFromStart = st;
			Clip.TrimTimeFromEnd = Clip.OriginalDuration - et;

			await SaveClip();
		}

		public async Task setRawVideo(string path)
		{
			rawVideoFilePath = path;
			RawVideo = await StorageFile.GetFileFromPathAsync(path);
			Clip = await MediaClip.CreateFromFileAsync(RawVideo);
		}

		public async Task SaveClip()
		{
			
			var composition = new MediaComposition();
			composition.Clips.Add(Clip.Clone());

			var EditedVideo = await KnownFolders.VideosLibrary.CreateFileAsync("temp_edited_video.mp4", CreationCollisionOption.ReplaceExisting);
			await composition.RenderToFileAsync(EditedVideo, MediaTrimmingPreference.Precise);
			editedVideoFilePath = EditedVideo.Path;
		}

		public double DurationInSeconds() { return Math.Round(Clip.OriginalDuration.TotalSeconds, 1); }
		
	}
}
