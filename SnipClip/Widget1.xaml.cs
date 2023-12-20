using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml.Data;
using System.ComponentModel;
using Windows.UI.Xaml.Media;
using System.Diagnostics;


namespace SnipClip
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	/// 
	public sealed partial class Widget1 : Page
	{
		StorageFolder rootFolderPath;

		List<FileListItem> fileList = new List<FileListItem>();

		VideoEditor editor = new VideoEditor();

		ViewModel VideoController = new ViewModel();

		DispatcherTimer rangeSelectorDragTimer = new DispatcherTimer();

		double ogStart, ogEnd = 0;
		public Widget1()
		{
			this.InitializeComponent();


			SetRootDir();
		}

		private async void SetRootDir()
		{
			StorageFolder BASE_DIR = KnownFolders.VideosLibrary;

			rootFolderPath = await BASE_DIR.TryGetItemAsync("Captures") as StorageFolder;

			await PopulateFileList();
			resetSelectionTools();

		}

		private async Task PopulateFileList()
		{
			var rootFiles = await rootFolderPath.GetFilesAsync();
			var files = rootFiles.Reverse().ToArray();
			

			foreach (var file in files)
			{
				FileListItem fileListItem = new FileListItem(file.DisplayName, file.Path, file.DateCreated.LocalDateTime, await ConvertThumbnailToBitmap(await file.GetThumbnailAsync(ThumbnailMode.SingleItem)));

				fileList.Add(fileListItem);
			}

			fileListView.ItemsSource = fileList;

			progressRing.Visibility = Visibility.Visible;

			await editor.setRawVideo(files[0].Path);
			SetNewStreamSource(editor.rawVideoFilePath);

			progressRing.Visibility = Visibility.Collapsed;
			
		}

		public async Task<BitmapImage> ConvertThumbnailToBitmap(IRandomAccessStream thumbnailStream)
		{
			try
			{
				var bitmapImage = new BitmapImage();
				await bitmapImage.SetSourceAsync(thumbnailStream);

				return bitmapImage;
			}
			catch (Exception ex)
			{
				// Handle exceptions
				Console.WriteLine($"Error: {ex.Message}");
			}

			return null;
		}

		private async void SetNewStreamSource(string path)
		{
			var videoFile = await StorageFile.GetFileFromPathAsync(path);

			if (videoFile != null)
			{
				var stream = await videoFile.OpenAsync(FileAccessMode.Read);
				videoPreview.SetSource(stream, videoFile.ContentType);
			}
		}

		private async void fileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Handle the selection change here
			if (fileListView.SelectedItem != null)
			{
				int selectedIndex = fileListView.SelectedIndex;

				await editor.setRawVideo(fileList[selectedIndex].FilePath);

				SetNewStreamSource(editor.rawVideoFilePath);

				resetSelectionTools();
			}
		}

		private void Open_Folder_Click(object sender, RoutedEventArgs e)
		{

		}


		private void RangeSelector_DragStarted(object sender, DragStartedEventArgs e)
		{
			videoPreview.Pause();
			videoPreview.AreTransportControlsEnabled = false;

			ogStart = VideoController.Start;
			ogEnd = VideoController.End;
			
			rangeSelectorDragTimer.Interval = TimeSpan.FromSeconds(.01);
			rangeSelectorDragTimer.Tick += DragTimeTick;

			rangeSelectorDragTimer.Start();
		}

		private void VideoTimeRangeSelector_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
		{
			videoPreview.AreTransportControlsEnabled = true;

			rangeSelectorDragTimer.Stop();
			videoPreview.Position = TimeSpan.FromSeconds(VideoController.Start);
		}

		private void DragTimeTick(object sender, object e)
		{
			if(VideoController.Start != ogStart)
			{
				videoPreview.Position = TimeSpan.FromSeconds(VideoController.Start);
				StartTimeInput.Text = Math.Round(VideoController.Start, 2).ToString();
			}
			else if(VideoController.End != ogEnd)
			{
				EndTimeInput.Text = Math.Round(VideoController.End, 2).ToString();
				videoPreview.Position = TimeSpan.FromSeconds(VideoController.End);
			}
		}

		private void videoPreview_MediaOpened(object sender, RoutedEventArgs e)
		{
			
		}

		private void videoPreview_MediaStateChanged(object sender, RoutedEventArgs e)
		{
			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(.1);
			timer.Tick += VideoTimeTick;

			if (videoPreview.CurrentState == MediaElementState.Playing)
			{
				if (videoPreview.Position.TotalSeconds < VideoController.Start)
				{
					videoPreview.Position = TimeSpan.FromSeconds(VideoController.Start);
				}
				
				timer.Start();
			}

			if (videoPreview.CurrentState == MediaElementState.Paused)
			{
				timer.Stop();
			}


		}
		
		private void VideoTimeTick(object sender, object e)
		{
			// Check the current position and stop playback if it exceeds the end time
			if (videoPreview.Position.TotalSeconds > VideoController.End)
			{
				videoPreview.Position = TimeSpan.FromSeconds(VideoController.Start);
				videoPreview.Pause();
			}
		}

		private void resetSelectionTools()
		{
			VideoController.Duration = editor.DurationInSeconds();
			EndTimeInput.Text = VideoController.Duration.ToString();

			VideoController.Start = 0;
			VideoController.End = VideoController.Duration;

			StartTimeInput.Text = Math.Round(VideoController.Start, 2).ToString();
			EndTimeInput.Text = Math.Round(VideoController.End, 2).ToString();
		}

		private async void StartTimeInput_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (StartTimeInput.Text != "")
				VideoController.Start = Double.Parse(StartTimeInput.Text);


			videoPreview.Position = TimeSpan.FromSeconds(VideoController.Start);
		}

		private async void EndTimeInput_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (EndTimeInput.Text != "")
				VideoController.End = Double.Parse(EndTimeInput.Text);

		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{

			progressRing.Visibility = Visibility.Visible;
			videoPreview.IsHitTestVisible = false;

			videoPreview.Stop();
			videoPreview.Source = null;
			await editor.TrimClipAsync(VideoController.Start, VideoController.End);
			SetNewStreamSource(editor.editedVideoFilePath);

			videoPreview.IsHitTestVisible = true;
			progressRing.Visibility = Visibility.Collapsed;


		}

		private void RangeSelector_ValueChanged(object sender, RangeChangedEventArgs e)
		{
			StartTimeInput.Text = Math.Round(VideoController.Start, 2).ToString();
			EndTimeInput.Text= Math.Round(VideoController.End, 2).ToString();

			
		}

		
	}
}
