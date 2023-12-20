using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace SnipClip
{
	internal class FileListItem
	{
		public string FileName { get; set; }
		public string FilePath { get; set; }
		public BitmapImage Thumbnail { get; set; }
		public DateTime CreationDateTime { get; set; }

		public FileListItem(string fileName, string filePath, DateTime creationDateTime, BitmapImage thumbnail)
		{
			FileName = fileName;
			FilePath = filePath;
			CreationDateTime = creationDateTime;
			Thumbnail = thumbnail;
		}
	}
}
