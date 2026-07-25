using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class CustomersController : Controller
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public CustomersController(ICustomerRepository customerRepository, IVehicleRepository vehicleRepository)
    {
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
    }

    // GET /Customers
    public async Task<IActionResult> Index()
    {
        var customers = await _customerRepository.GetAllWithVehiclesAsync();

        var viewModel = customers.Select(c => new CustomerRowViewModel
        {
            Id = c.CustomerId,
            FullName = c.FullName,
            Email = c.Email,
            Phone = c.Phone,
            VehicleCount = c.Vehicles.Count
        }).ToList();

        return View(viewModel);
    }

    // GET /Customers/Create
    public IActionResult Create()
    {
        return View(new CreateCustomerViewModel());
    }

    // POST /Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var customer = new Customer
        {
            FullName = model.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "A customer with that email already exists.");
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
    }

    // GET /Customers/Details/{id}
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerRepository.GetByIdWithVehiclesAsync(id);
        if (customer == null)
            return NotFound();

        return View(BuildDetailsViewModel(customer));
    }

    // POST /Customers/AddVehicle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVehicle(int customerId, string make, string model, short year, string? vin, string? licensePlate)
    {
        var customer = await _customerRepository.GetByIdWithVehiclesAsync(customerId);
        if (customer == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
        {
            var viewModel = BuildDetailsViewModel(customer);
            viewModel.ErrorMessage = "Make and model are required.";
            return View(nameof(Details), viewModel);
        }

        var vehicle = new Vehicle
        {
            CustomerId = customerId,
            Make = make.Trim(),
            Model = model.Trim(),
            Year = year,
            Vin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim(),
            LicensePlate = string.IsNullOrWhiteSpace(licensePlate) ? null : licensePlate.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _vehicleRepository.AddAsync(vehicle);
            await _vehicleRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var viewModel = BuildDetailsViewModel(customer);
            viewModel.ErrorMessage = "A vehicle with that VIN already exists.";
            return View(nameof(Details), viewModel);
        }

        return RedirectToAction(nameof(Details), new { id = customerId });
    }

    private static CustomerDetailsViewModel BuildDetailsViewModel(Customer customer)
    {
        return new CustomerDetailsViewModel
        {
            CustomerId = customer.CustomerId,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            Vehicles = customer.Vehicles.Select(v => new VehicleRowViewModel
            {
                Id = v.VehicleId,
                Year = v.Year,
                Make = v.Make,
                Model = v.Model,
                Vin = v.Vin,
                LicensePlate = v.LicensePlate
            }).ToList()
        };
    }
}
