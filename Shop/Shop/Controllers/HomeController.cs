using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shop.Models;

namespace Shop.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var featuredProducts = _context.Products.Take(4).ToList();
        return View(featuredProducts);
    }
    public IActionResult Products()
    {
        var products = _context.Products.ToList();
        return View(products);
    }

    public IActionResult ProductDetails()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

}
