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
using Windows.Media.Editing;
using System.Drawing;
using System.IO;
using OpenCvSharp;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Animation;
using System.ServiceModel.Channels;
using Windows.System;



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

		string filename = "SnippedClip";

		double ogStart, ogEnd = 0;
		public Widget1()
		{
			this.InitializeComponent();

			editor.WidgetContext = this;

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
			var files = rootFiles.OrderByDescending(file => file.DateCreated.LocalDateTime).ToArray();

			foreach (var file in files)
			{
				FileListItem fileListItem = new FileListItem(file.DisplayName, file.Path, file.DateCreated.LocalDateTime, await ConvertThumbnailToBitmap(await file.GetThumbnailAsync(ThumbnailMode.SingleItem)));
				fileList.Add(fileListItem);
			}

			fileListView.ItemsSource = fileList;

			if(files.Length > 0)
			{
				await editor.setRawVideo(files[0].Path);
				SetNewStreamSource(editor.rawVideoFilePath);
				trimButton.IsEnabled = true;
				VideoTimeRangeSelector.IsEnabled = true;
				SaveButton.IsEnabled = true;
			}
			else
			{
				trimButton.IsEnabled = false;
				VideoTimeRangeSelector.IsEnabled = false;
				SaveButton.IsEnabled = false;
			}
			

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
			resetSelectionTools();
		}

		private async void fileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Handle the selection change here
			if (fileListView.SelectedItem != null)
			{
				int selectedIndex = fileListView.SelectedIndex;

				await editor.setRawVideo(fileList[selectedIndex].FilePath);

				SetNewStreamSource(editor.rawVideoFilePath);
			}
		}

		private async void Open_Folder_Click(object sender, RoutedEventArgs e)
		{
			await Launcher.LaunchFolderAsync(rootFolderPath);
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
			if (VideoController.Start != ogStart)
			{
				videoPreview.Position = TimeSpan.FromSeconds(VideoController.Start);
				StartTimeInput.Text = Math.Round(VideoController.Start, 2).ToString("F2");
			}
			else if (VideoController.End != ogEnd)
			{
				EndTimeInput.Text = Math.Round(VideoController.End, 2).ToString("F2");
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
			EndTimeInput.Text = VideoController.Duration.ToString("F2");


			VideoController.Start = 0;
			VideoController.End = VideoController.Duration;

			StartTimeInput.Text = Math.Round(VideoController.Start, 2).ToString("F2");
			EndTimeInput.Text = Math.Round(VideoController.End, 2).ToString("F2");
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

		private async void TirmButton_Click(object sender, RoutedEventArgs e)
		{

			progressRing.Visibility = Visibility.Visible;
			videoPreview.IsHitTestVisible = false;

			videoPreview.Stop();
			videoPreview.Source = null;
			await editor.TrimClipAsync(VideoController.Start, VideoController.End);
			
			SetNewStreamSource(editor.wipVideoFilePath);

			videoPreview.IsHitTestVisible = true;
			progressRing.Visibility = Visibility.Collapsed;
			resetSelectionTools();

		}

		private async void ChangeSaveFolder_Click(object sender, RoutedEventArgs e)
		{
			FolderPicker fp = new FolderPicker();

			fp.SuggestedStartLocation = PickerLocationId.VideosLibrary;
			fp.FileTypeFilter.Add("*");

			StorageFolder folder = await fp.PickSingleFolderAsync();
			if (folder != null)
			{
				// The user selected a folder
				// Perform actions with the selected folder
				editor.SetSaveFolder(folder);
			}
			else
			{
				// The user did not select a folder
			}
		}

		private async void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedItem = FileExtensionComboBox.SelectedItem as ComboBoxItem;
			string fileExtension = selectedItem.Content.ToString();

			await editor.SaveClip(filename, fileExtension);
			SetNewStreamSource(editor.rawVideoFilePath);

			Toast($"Saved {filename} sucessfully!");
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			filename = filenameTextbox.Text;
        }


		private void RangeSelector_ValueChanged(object sender, RangeChangedEventArgs e)
		{

			StartTimeInput.Text = Math.Round(VideoController.Start, 2).ToString("F2");
			EndTimeInput.Text = Math.Round(VideoController.End, 2).ToString("F2");
		}

		private async void OpenEditedVideoFolder(object sender, RoutedEventArgs e)
		{
			await Launcher.LaunchFolderAsync(editor.SaveFolder);

		}

		public void Toast(string msg)
		{
			ToastMessage.Text = msg;
			ShowHideToast.Begin();
		}
	}
}
