/*
    D - Furniture(decor) quality - based on what player put in eating zone
    F - Food quality - based on what appliances were used
    S - Speed of serving - based on how much staff is paid
    P - Price multiplier

    Additional 
    M - Marketing budget

    Attraction works as inverse of price, but rate of decrease is based on decoration
    Higher the price - less customers but those who come are willing to pay extra, but need quality(furniture, decor and food) to be satisfied
    Lower the price - more customers and they care less about quality
*/

using System;
using Godot;

/**<summary> Class for storing and calculating various customer attraction values</summary>*/
public class Attraction
{
    /**<summary>Value changed by player. Defines how much more customers need to pay for eating in your cafe</summary>*/
    private float _priceMultiplier = 1f;

    private float _decorationQuality = 1f;

    private float _staffRating = 1f;

    private float _foodQuality = 1f;

    private float _marketingBudget = 100f;

    //honestly, this some random function that i put very little thought in
    public int CustomerAttraction => (int)((_decorationQuality + _foodQuality + _staffRating) / _marketingBudget / (100 - _priceMultiplier));

    /**<summary>Min level of customer that will come to cafe</summary>*/
    public int CustomerLowestQuality => (int)_priceMultiplier;

    /**<summary>Max level of customer that will come to cafe</summary>*/
    public int CustomerHighestQuality => (int)(_priceMultiplier + 1/*min level*/ + Mathf.Log(_decorationQuality));

    /**<summary>How much customers are satisfied.<para/> Multiply this by price and you get value for tips</summary>*/
    public int CustomerSatisfaction => (int)((_decorationQuality + _foodQuality + _staffRating) / (_priceMultiplier));

    public float PriceMultiplier { get => _priceMultiplier; set => _priceMultiplier = value; }
    public float DecorationQuality { get => _decorationQuality; set => _decorationQuality = value; }
    public float StaffRating { get => _staffRating; set => _staffRating = value; }
    public float FoodQuality { get => _foodQuality; set => _foodQuality = value; }
    public float MarketingBudget { get => _marketingBudget; set => _marketingBudget = value; }
}