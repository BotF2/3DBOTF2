using UnityEngine;

public class AIData
    {
    public string Name;
    public string Description;
    public int Level;
    public float Health;
    public float AttackPower;
    public float DefensePower;
    // Constructor to initialize AI data
    public AIData(string name, string description, int level, float health, float attackPower, float defensePower)
    {
        Name = name;
        Description = description;
        Level = level;
        Health = health;
        AttackPower = attackPower;
        DefensePower = defensePower;
    }
    // Method to display AI data
    public void DisplayData()
    {
        Debug.Log($"Name: {Name}, Description: {Description}, Level: {Level}, Health: {Health}, Attack Power: {AttackPower}, Defense Power: {DefensePower}");
    }
}

