using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnipClip
{
	public class ViewModel : INotifyPropertyChanged
	{
		private double duration;

		public double Duration
		{
			get { return duration; }
			set
			{
				if (duration != value)
				{
					duration =  value;
					OnPropertyChanged(nameof(Duration));
				}
			}
		}

		private double startTime;
		public double Start
		{
			get { return startTime; }
			set
			{
				if (startTime != value)
				{
					startTime = value;
					OnPropertyChanged(nameof(Start));
				}
			}
		}

		private double endTime;
		public double End
		{
			get { return endTime; }
			set
			{
				if (endTime != value)
				{
					endTime = value;
					OnPropertyChanged(nameof(End));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		internal virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
