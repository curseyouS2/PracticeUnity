using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameState
{
    public int currentDay = 1;
    public string currentTimeSlot = "afternoon"; // "afternoon" or "night"
    public int maxDays = 30;
    public Stats stats = new Stats();
    public Conditions conditions = new Conditions();
}

[System.Serializable]
public class Stats
{
    public int intelligence = 10;
    public int charm = 10;
    public int courage = 10;
    public int fatigue = 0;
    public int money = 5000;
}

[System.Serializable]
public class Conditions
{
    public bool isExhausted = false;
}

[System.Serializable]
public class Location
{
    public string id;
    public string name;
    public List<string> availableTime;
    public List<Activity> activities;
}

[System.Serializable]
public class Activity
{
    public string id;
    public string name;
    public StatChanges statChanges;
    public int cost;
    public Dictionary<string, int> requiredStats;
    public string description;
}

[System.Serializable]
public class StatChanges
{
    public int intelligence;
    public int charm;
    public int courage;
    public int fatigue;
    public int money;
}

[System.Serializable]
public class LocationDataWrapper
{
    public List<Location> locations;
}

[System.Serializable]
public class Ending
{
    public string id;
    public string name;
    public Dictionary<string, int> conditions;
    public int priority;
    public string description;
}

[System.Serializable]
public class EndingDataWrapper
{
    public List<Ending> endings;
}