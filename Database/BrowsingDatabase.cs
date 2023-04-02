﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteDB;
using Yarrow.Extensions;
using Yarrow.Interfaces;
using Yarrow.Models;

namespace Yarrow.Database;

internal class BrowsingDatabase : IBrowsingDatabase
{
    private readonly ILiteCollection<Bookmark> _bookmarksStore;
    private readonly ILiteCollection<Visited> _visitedStore;

    private ObservableCollection<Bookmark> _bookmarks;
    private ObservableCollection<Visited> _visited;

    public BrowsingDatabase(ILiteDatabase database)
    {
        _bookmarksStore = database.GetCollection<Bookmark>();
        _bookmarksStore.EnsureIndex(b => b.Url);

        _visitedStore = database.GetCollection<Visited>();

        Bookmarks = new ObservableCollection<Bookmark>(_bookmarksStore.FindAll());
        Visited = new ObservableCollection<Visited>(_visitedStore.FindAll());
    }

    public ObservableCollection<Bookmark> Bookmarks
    {
        get => _bookmarks;
        set
        {
            if (Equals(value, _bookmarks))
                return;

            if (_bookmarks != null)
                _bookmarks.CollectionChanged -= Bookmarks_CollectionChanged;

            _bookmarks = value;
            _bookmarks.CollectionChanged += Bookmarks_CollectionChanged;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Visited> Visited
    {
        get => _visited;
        set
        {
            if (Equals(value, _visited))
                return;

            if (_visited != null)
                _visited.CollectionChanged -= Visited_CollectionChanged;

            _visited = value;
            _visited.CollectionChanged += Visited_CollectionChanged;
            OnPropertyChanged();
        }
    }

    public bool IsBookmark(Uri location, out Bookmark found)
    {
        found = _bookmarks.FirstOrDefault(b =>
        {
            var bookmarkUrl = b.Url.ToGeminiUri();
            return bookmarkUrl.Host.Equals(location.Host, StringComparison.InvariantCultureIgnoreCase) &&
                   bookmarkUrl.PathAndQuery.Equals(location.PathAndQuery);
        });

        return found != null;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void Visited_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                foreach (var entity in e.NewItems.Cast<Visited>())
                    entity.Id = _visitedStore.Insert(entity);
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                foreach (var entity in e.NewItems.Cast<Visited>())
                    _visitedStore.Delete(entity.Id);
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                throw new NotImplementedException();
            case NotifyCollectionChangedAction.Reset:
                Visited = new ObservableCollection<Visited>(_visitedStore.FindAll());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Bookmarks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                foreach (var entity in e.NewItems.Cast<Bookmark>())
                    entity.Id = _bookmarksStore.Insert(entity);
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                foreach (var entity in e.OldItems.Cast<Bookmark>())
                    _bookmarksStore.Delete(entity.Id);
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                throw new NotImplementedException();
            case NotifyCollectionChangedAction.Reset:
                Bookmarks = new ObservableCollection<Bookmark>(_bookmarksStore.FindAll());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}