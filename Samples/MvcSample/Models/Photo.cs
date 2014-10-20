

using System.Web.Hosting;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Specialized;
using System.Web;
using ImageResizer.Util;
using System.Threading;
using ImageResizer;
using ImageResizer.Configuration;
using System.Runtime.Serialization;
using System.Xml;
[DataContract]
public class Photo {

    

    public Photo(HttpPostedFileBase file, NameValueCollection defaultQuery =null, NameValueCollection preprocessingQuery = null) {
        Id = Guid.NewGuid();
        OriginalName = file.FileName;
        string newPath = PathUtils.SetExtension(Id.ToString(),PathUtils.GetExtension(OriginalName));
        FilePath = newPath;

        if (!Directory.Exists(StoragePath))Directory.CreateDirectory(StoragePath);

        if (preprocessingQuery == null || !Config.Current.Pipeline.IsAcceptedImageType(OriginalName)) {
            file.SaveAs(Path.Combine(StoragePath, newPath));
        } else {
            var j = new ImageJob(file, Path.Combine(StoragePath, Id.ToString()), new ResizeSettings(preprocessingQuery));
            j.AddFileExtension = true;
            j.Build();
            FilePath = Path.GetFileName(j.FinalPath);

        }
        Query = defaultQuery != null ? defaultQuery : new NameValueCollection();
    }

    [DataMember]
    public Guid Id { get; set; }
    [DataMember]
    public string FilePath { get; set; }

    [DataMember]
    public string Caption { get; set; }

    [DataMember]
    public string OriginalName { get; set; }
    [IgnoreDataMember]
    public NameValueCollection Query { get; set; }
    [DataMember]
    public String Querystring { get { return PathUtils.BuildQueryString(Query, true); } set{ Query = PathUtils.ParseQueryStringFriendlyAllowSemicolons(value);}}

    public string UrlWith(string settings) {
        return PathUtils.MergeOverwriteQueryString("/photos/" + FilePath + Querystring, PathUtils.ParseQueryStringFriendlyAllowSemicolons(settings));
    }

    public void Delete() {
        Photo.Remove(Id);
    }

    public void Save() {
        Photo.Update(Id, this);
    }
    protected Photo(){}
    public Photo DeepCopy() {
        var p = new Photo();
        p.Id = Id;
        p.FilePath = FilePath;
        p.OriginalName = OriginalName;
        p.Query = new NameValueCollection(Query);
        return p;
    }


    public static void Remove(Guid photo){
        EnsureLoaded();
        _syncData.EnterWriteLock();
        try{
             _allPhotos.Remove(_byGuid[photo]);
            _byGuid.Remove(photo);
            SaveData(_allPhotos);
           
        }finally{
            _syncData.ExitWriteLock();
        }
    }
    public static void Add(Photo photo){
        EnsureLoaded();
        photo = photo.DeepCopy();
        _syncData.EnterWriteLock();
        try{
            _allPhotos.Add(photo);
            _byGuid[photo.Id] = photo;
            SaveData(_allPhotos);
        }finally{
            _syncData.ExitWriteLock();
        }
    }

    public static void Update(Guid id, Photo photo){
        EnsureLoaded();
        photo = photo.DeepCopy();
        _syncData.EnterWriteLock();
        try{
             if (_byGuid.ContainsKey(id)) _allPhotos.Remove(_byGuid[id]);
             _allPhotos.Add(photo);
            _byGuid[id] = photo;
            SaveData(_allPhotos);
        }finally{
            _syncData.ExitWriteLock();
        }
    }

    

    public static Photo Get(Guid id) {
        EnsureLoaded();
        _syncData.EnterReadLock();
        try {
            Photo result;
            if (_byGuid.TryGetValue(id, out result)) return result.DeepCopy();
            return null;
        } finally {
            _syncData.ExitReadLock();
        }
    }


    public static Photo[] GetAll() {
        EnsureLoaded();
        _syncData.EnterReadLock();
        try {
            return _allPhotos.ToArray();
        } finally {
            _syncData.ExitReadLock();
        }
    }


    public static string StoragePath {
        get {
            return HostingEnvironment.MapPath("~/App_Data/Photos");
        }
    }
    public static string XmlPath {
        get {
            return HostingEnvironment.MapPath("~/App_Data/Photos.xml");
        }
    }

    private static List<Photo> _allPhotos = null;
    private static IDictionary<Guid, Photo> _byGuid = null;

    private static ReaderWriterLockSlim _syncData = new ReaderWriterLockSlim( LockRecursionPolicy.SupportsRecursion);
    private static object _syncLoad = new object();


    private static void Index(IList<Photo> photos){
        if (_byGuid == null) _byGuid = new Dictionary<Guid, Photo>(photos.Count);
        _byGuid.Clear();
        foreach (Photo p in photos) {
            _byGuid[p.Id] = p;
        }
    }
    private static List<Photo> LoadData() {
        if (!File.Exists(XmlPath)) return new List<Photo>();
        var des = new DataContractSerializer(typeof(List<Photo>));
        using (var fs = new FileStream(XmlPath, FileMode.Open, FileAccess.Read)) {
            try {
                return (List<Photo>)des.ReadObject(fs);
            } catch (XmlException) {
                return new List<Photo>();
            }
        }
    }

    private static void SaveData(List<Photo> data) {
        if (!Directory.Exists(Path.GetDirectoryName(XmlPath))) Directory.CreateDirectory(Path.GetDirectoryName(XmlPath));
        var ser = new DataContractSerializer(typeof(List<Photo>));
        using (var fs = new FileStream(XmlPath, FileMode.Create, FileAccess.Write)) {
            ser.WriteObject(fs, data);
        }
    }

    private static void EnsureLoaded() {
        if (_allPhotos == null) {
            lock (_syncLoad) {
                var temp = LoadData();
                Index(temp);
                _allPhotos = LoadData();
            }
        }
    }

    private static IList<Photo> AllPhotos {
        get {
            EnsureLoaded();
            return _allPhotos;
        }
    }


}