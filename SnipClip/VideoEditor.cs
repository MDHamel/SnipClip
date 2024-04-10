using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Controls;

namespace SnipClip
{
	internal class VideoEditor
	{

		public StorageFile RawVideo { get; set; }

		public MediaClip Clip { get; set; }

		public Widget1 WidgetContext { get; set; }

		public string rawVideoFilePath;
		public string wipVideoFilePath;

		static StorageFolder tempFolder;
		public StorageFolder SaveFolder ;
		public string save_path;

		bool saving = false;

		public VideoEditor() {
			Task.Run(async () => await SetupFolders());
		}

		private async Task SetupFolders()
		{
			var settings =	ApplicationData.Current.LocalSettings;
			save_path = (string) settings.Values["save_path"];

			if (save_path == null)
			{
				try
				{
					SaveFolder = await KnownFolders.VideosLibrary.GetFolderAsync("Snipped Clips");
				}
				catch (FileNotFoundException)
				{
					SaveFolder = await KnownFolders.VideosLibrary.CreateFolderAsync("Snipped Clips");
				}

				save_path = SaveFolder.Path;
				settings.Values["save_path"] = save_path;
			}
			else
			{
				try
				{
					// Attempt to get the folder from the given path
					SaveFolder = await StorageFolder.GetFolderFromPathAsync(save_path);
				}
				catch (FileNotFoundException)
				{
					// The folder does not exist, attempt to create it
					SaveFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
						Path.GetFileName(save_path), // Use the folder name only
						CreationCollisionOption.OpenIfExists);
				}
				catch (UnauthorizedAccessException ex)
				{
					// Handle the case where the app does not have the required permissions
					// This could involve requesting permissions or informing the user
					WidgetContext.Toast("You Don't Have Permission.");
					throw new Exception("Your app does not have the necessary permissions.", ex);
					
				}
			}

			TempFolderGenerator();
		}

		public async Task SetSaveFolder(StorageFolder folder)
		{
			SaveFolder = folder;
			save_path = SaveFolder.Path;

			var settings = ApplicationData.Current.LocalSettings;

			settings.Values["save_path"] = save_path;
			TempFolderGenerator() ;
		}

		private async void TempFolderGenerator()
		{
			try
			{
				tempFolder = await SaveFolder.GetFolderAsync("temp");
				await DeleteTempFolderContents();
			}
			catch (FileNotFoundException)
			{
				tempFolder = await SaveFolder.CreateFolderAsync("temp");
			}
		}

		public async Task TrimClipAsync(double start, double end)
		{	
			// Set the start and end points for cropping (adjust as needed)
			TimeSpan st = TimeSpan.FromSeconds(start); // Replace with your desired start time
			TimeSpan et = TimeSpan.FromSeconds(end);   // Replace with your desired end time

			// Trim the clip to the specified time range
			Clip.TrimTimeFromStart = st;
			try
			{
				Clip.TrimTimeFromEnd = Clip.OriginalDuration.Subtract(et);
			}
			catch (Exception ex)
			{
				Clip.TrimTimeFromEnd = TimeSpan.FromSeconds(0);
			}

			await SaveTempClip();
		}

		public async Task setRawVideo(string path)
		{

			rawVideoFilePath = path;
			wipVideoFilePath = path;
			RawVideo = await StorageFile.GetFileFromPathAsync(path);
			Clip = await MediaClip.CreateFromFileAsync(RawVideo);

		}

		public async Task SaveTempClip()
		{
			try
			{
				var composition = new MediaComposition();
				composition.Clips.Add(Clip.Clone());

				var EditedVideo = await tempFolder.CreateFileAsync("temp_video.mp4", CreationCollisionOption.GenerateUniqueName);

				await composition.RenderToFileAsync(EditedVideo, MediaTrimmingPreference.Precise);
				wipVideoFilePath = EditedVideo.Path;

				Clip = await MediaClip.CreateFromFileAsync(EditedVideo);
				saving = false;

			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

		}

		public async Task SaveClip(string file_name, string file_extension)
		{
			var composition = new MediaComposition();
			composition.Clips.Add(Clip.Clone());

			var video = await SaveFolder.CreateFileAsync(file_name + file_extension, CreationCollisionOption.GenerateUniqueName);
			await composition.RenderToFileAsync(video, MediaTrimmingPreference.Precise);
			await DeleteTempFolderContents();
			await setRawVideo(rawVideoFilePath);
		}

		public static async Task DeleteTempFolderContents()
		{
			foreach (var item in await tempFolder.GetFilesAsync())
			{
				await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}

		public double DurationInSeconds() { 
			if(Clip != null)
				return Math.Round(Clip.OriginalDuration.TotalSeconds, 1); 
			return 0;
		}
		
	}
}
