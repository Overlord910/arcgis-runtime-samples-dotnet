﻿using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.WebMap;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArcGISRuntime.Samples.Desktop
{
	/// <summary>
	/// This sample demonstrates adding data a WebMap from ArcGIS Online to an application using the MVVM design pattern.
	/// </summary>
	/// <title>Load WebMap MVVM</title>
	/// <category>Portal</category>
	public partial class LoadWebMapMVVM : UserControl
	{
		/// <summary>Construct Load WebMap sample control</summary>
		public LoadWebMapMVVM()
		{
			InitializeComponent();
			DataContext = new LoadWebMapVM();
		}

		private void searchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems == null || e.AddedItems.Count == 0)
				return;

			// Forward from View to ViewModel
			if (((LoadWebMapVM)DataContext).LoadWebMapCommand.CanExecute(e.AddedItems[0]))
				((LoadWebMapVM)DataContext).LoadWebMapCommand.Execute(e.AddedItems[0]);
		}
	}


	// LoadWebMap MVVM - View Model
	internal class LoadWebMapVM : INotifyPropertyChanged
	{
		// Is Searching / Loading Indicator
		private bool _isBusy;
		public bool IsBusy
		{
			get { return _isBusy; }
			set
			{
				_isBusy = value;
				RaisePropertyChanged("IsBusy");
			}
		}

		// User entered web map ID
		private string _searchText;
		public string SearchText
		{
			get { return _searchText; }
			set
			{
				_searchText = value;
				RaisePropertyChanged("SearchText");
			}
		}

		// Portal Search Results
		private ObservableCollection<ArcGISPortalItem> _searchResults;
		public ObservableCollection<ArcGISPortalItem> SearchResults
		{
			get { return _searchResults; }
			set
			{
				_searchResults = value;
				RaisePropertyChanged("SearchResults");
			}
		}

		// Currently Selected WebMapViewModel
		private WebMapViewModel _currentWebMapVM;
		public WebMapViewModel CurrentWebMapVM
		{
			get { return _currentWebMapVM; }
			set
			{
				_currentWebMapVM = value;
				RaisePropertyChanged("CurrentWebMapVM");
			}
		}

		// Selected ArcGIS Portal Item
		private ArcGISPortalItem _loadedPortalItem;
		public ArcGISPortalItem LoadedPortalItem
		{
			get { return _loadedPortalItem; }
			set
			{
				_loadedPortalItem = value;
				RaisePropertyChanged("LoadedPortalItem");
			}
		}

		// ICommand to initiate arcgis.com search
		public ICommand SearchCommand { get; private set; }

		// ICommand to load the current webmap
		public ICommand LoadWebMapCommand { get; private set; }

		private ArcGISPortal _portal;

		public LoadWebMapVM()
		{
			IsBusy = false;
			SearchResults = null;
			CurrentWebMapVM = null;
			SearchText = string.Empty;
			LoadedPortalItem = null;

			SearchCommand = new DelegateCommand(async p => await SearchArcgisOnline());
			LoadWebMapCommand = new DelegateCommand(async p => await LoadWebMapAsync(p as ArcGISPortalItem));

			var task = GetFeaturedWebMapsAsync();
		}

		// Loads featured WebMaps from ArcGIS online
		private async Task GetFeaturedWebMapsAsync()
		{
			try
			{
				IsBusy = true;

				if (_portal == null)
					_portal = await ArcGISPortal.CreateAsync();

				var searchParams = new SearchParameters("type: \"web map\" NOT \"web mapping application\" ")
				{
					Limit = 20,
					SortField = "avgrating",
					SortOrder = QuerySortOrder.Descending,
				};
				var result = await _portal.ArcGISPortalInfo.SearchFeaturedItemsAsync();

				SearchResults = new ObservableCollection<ArcGISPortalItem>(result.Results);
			}
			finally
			{
				IsBusy = false;
			}
		}

		// Searches ArcGIS online for webmaps containing SearchText
		private async Task SearchArcgisOnline()
		{
			try
			{
				IsBusy = true;

				if (_portal == null)
					_portal = await ArcGISPortal.CreateAsync();

				var searchParams = new SearchParameters(SearchText + " type: \"web map\" NOT \"web mapping application\" ")
				{
					Limit = 20,
					SortField = "avgrating",
					SortOrder = QuerySortOrder.Descending,
				};
				var result = await _portal.SearchItemsAsync(searchParams);

				SearchResults = new ObservableCollection<ArcGISPortalItem>(result.Results);
			}
			finally
			{
				IsBusy = false;
			}
		}

		// Loads the given webmap
		private async Task LoadWebMapAsync(ArcGISPortalItem portalItem)
		{
			try
			{
				IsBusy = true;

				if (_portal == null)
					_portal = await ArcGISPortal.CreateAsync();

				var webmap = await WebMap.FromPortalItemAsync(portalItem);
				LoadedPortalItem = portalItem;
				CurrentWebMapVM = await WebMapViewModel.LoadAsync(webmap, _portal);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Sample Error");
			}
			finally
			{
				IsBusy = false;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void RaisePropertyChanged(string name)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		// DelegateCommand helper class
		internal class DelegateCommand : ICommand
		{
			Func<object, bool> _canExecute;
			Action<object> _executeAction;
			bool _canExecuteCache;

			public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecute = null)
			{
				this._executeAction = executeAction;
				this._canExecute = canExecute;
			}

			public bool CanExecute(object parameter)
			{
				if (_canExecute == null)
					return true;

				bool temp = _canExecute(parameter);
				if (_canExecuteCache != temp)
				{
					_canExecuteCache = temp;
					if (CanExecuteChanged != null)
						CanExecuteChanged(this, new EventArgs());
				}

				return _canExecuteCache;
			}

			public event EventHandler CanExecuteChanged;

			public void Execute(object parameter)
			{
				_executeAction(parameter);
			}
		}
	}
}
