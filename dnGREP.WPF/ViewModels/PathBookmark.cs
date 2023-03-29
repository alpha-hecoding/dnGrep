﻿using System;

namespace dnGREP.WPF
{
    public class PathBookmark : CultureAwareViewModel, IEquatable<PathBookmark>
    {
        public PathBookmark(string path)
        {
            SearchPath = path;
        }


        private string searchPath = string.Empty;
        public string SearchPath
        {
            get { return searchPath; }
            set
            {
                if (searchPath == value)
                {
                    return;
                }

                searchPath = value;
                OnPropertyChanged(nameof(SearchPath));
            }
        }


        private bool isPinned = false;
        public bool IsPinned
        {
            get { return isPinned; }
            set
            {
                if (isPinned == value)
                {
                    return;
                }

                isPinned = value;
                OnPropertyChanged(nameof(IsPinned));
                MainViewModel.MainViewMessenger.NotifyColleagues("IsPinnedChanged", this);
            }
        }


        public string FontFamily { get; set; } = SystemSymbols.FontFamily;

        public string DeleteCharacter { get; set; } = SystemSymbols.DeleteCharacter;

        public string PinCharacter { get; set; } = SystemSymbols.PinCharacter;

        public string UnpinCharacter { get; set; } = SystemSymbols.UnpinCharacter;

        public override int GetHashCode()
        {
            return SearchPath.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PathBookmark);
        }

        public bool Equals(PathBookmark other)
        {
            if (other == null)
            {
                return false;
            }
            return SearchPath.Equals(other.SearchPath);
        }
    }
}