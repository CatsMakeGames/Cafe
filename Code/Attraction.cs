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
using System.Linq;
using System.Collections.Generic;

/**<summary> Class for storing and calculating various customer attraction values</summary>*/
public class Attraction
{
    /**<summary>Value changed by player. Defines how much more customers need to pay for eating in your cafe</summary>*/
    private float _priceMultiplier = 1f;

    private float _decorationQuality = 1f;

    private float _staffPayment = 1f;

    private float _foodQuality = 1f;

    private float _marketingBudget = 100f;

    //honestly, this some random function that i put very little thought in
    public int CustomerAttraction => (int)((_decorationQuality + _foodQuality + _staffPayment) / _marketingBudget / (100 - _priceMultiplier));

    /**<summary>Min level of customer that will come to cafe</summary>*/
    public int CustomerLowestQuality => (int)_priceMultiplier;

    /**<summary>Max level of customer that will come to cafe</summary>*/
    public int CustomerHighestQuality => (int)(_priceMultiplier + 1/*min level*/ + Mathf.Log(_decorationQuality));

    /**<summary>How much customers are satisfied.<para/> Multiply this by price and you get value for tips</summary>*/
    public int CustomerSatisfaction => (int)((_decorationQuality + _foodQuality + _staffPayment) / (_priceMultiplier));

    public float PriceMultiplier { get => _priceMultiplier; set => _priceMultiplier = value; }
    public float DecorationQuality { get => _decorationQuality; set => _decorationQuality = value; }
    public float staffPayment { get => _staffPayment; set => _staffPayment = value; }
    public float FoodQuality { get => _foodQuality; set => _foodQuality = value; }
    public float MarketingBudget { get => _marketingBudget; set => _marketingBudget = value; }

    public List<float> GetSaveData()
    {
        List<float> res = new List<float>();
        res.Add(_priceMultiplier);//[0]
        res.Add(_marketingBudget);//[1]
        res.Add(_staffPayment);//[2]
        return res;
    }

    /**<summary>Recalculates values based on given furnitures</summary>*/
    public void Update(Cafe cafe)
    {
        //TODO: add other furniture typese
		float average = 0;
		var decorFurs =  cafe.Furnitures.Where(p=>p.Value.CurrentType == Furniture.FurnitureType.Table/*add other types that only customer sees here*/);
		decorFurs.ToList().ForEach(p=>average += p.Value.Level + 1);//+1 because level starts at 0
		_decorationQuality = average / decorFurs.Count();
		average = 0;
        //defining supported types via array is avoided due to how weirdly godot's arrays work in editor window
		decorFurs =  cafe.Furnitures.Where(p=>	p.Value.CurrentType == Furniture.FurnitureType.Fridge||
											    p.Value.CurrentType == Furniture.FurnitureType.Stove);
		decorFurs.ToList().ForEach(p=>average += p.Value.Level + 1);
		_foodQuality = average / decorFurs.Count();
    }
}