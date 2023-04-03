﻿namespace Yarrow.Interfaces;

public interface ISettingsDatabase
{
    string HomeUrl { get; set; }
    string LastVisitedUrl { get; set; }
    bool SaveVisited { get; set; }
}