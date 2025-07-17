﻿using LibParameters;

namespace SupportToolsData.Models;

public class RouteClassModel : ItemData
{
    public string Root { get; set; } = "api";
    public string Version { get; set; } = "v1";
    public string Base { get; set; }
}