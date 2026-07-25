using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

// Staff-only: managing customer/vehicle records isn't something a walk-in
// visitor should be able to do from the public site.
[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IAppointmentRepository _appointmentRepository;

    public CustomersController(ICustomerRepository customerRepository, IVehicleRepository vehicleRepository, IAppointmentRepository appointmentRepository)
    {
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _appointmentRepository = appointmentRepository;
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

    // GET /Customers/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerRepository.GetAsync(c => c.CustomerId == id);
        if (customer == null)
            return NotFound();

        return View(new EditCustomerViewModel
        {
            CustomerId = customer.CustomerId,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone
        });
    }

    // POST /Customers/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditCustomerViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var customer = await _customerRepository.GetAsync(c => c.CustomerId == model.CustomerId);
        if (customer == null)
            return NotFound();

        customer.FullName = model.FullName.Trim();
        customer.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        customer.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

        try
        {
            await _customerRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "A customer with that email already exists.");
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
    }

    // POST /Customers/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _customerRepository.GetByIdWithVehiclesAsync(id);
        if (customer == null)
            return NotFound();

        // Business rule: can't delete a customer that still has vehicles on file —
        // remove those first so history isn't silently orphaned.
        if (customer.Vehicles.Count > 0)
        {
            var viewModel = BuildDetailsViewModel(customer);
            viewModel.ErrorMessage = "Can't delete this customer while they still have vehicles on file. Remove the vehicles first.";
            return View(nameof(Details), viewModel);
        }

        _customerRepository.Delete(customer);
        await _customerRepository.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET /Customers/EditVehicle/{id}
    public async Task<IActionResult> EditVehicle(int id)
    {
        var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == id);
        if (vehicle == null)
            return NotFound();

        return View(new EditVehicleViewModel
        {
            VehicleId = vehicle.VehicleId,
            CustomerId = vehicle.CustomerId,
            Year = vehicle.Year,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Vin = vehicle.Vin,
            LicensePlate = vehicle.LicensePlate
        });
    }

    // POST /Customers/EditVehicle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVehicle(EditVehicleViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == viewModel.VehicleId);
        if (vehicle == null)
            return NotFound();

        vehicle.Year = viewModel.Year;
        vehicle.Make = viewModel.Make.Trim();
        vehicle.Model = viewModel.Model.Trim();
        vehicle.Vin = string.IsNullOrWhiteSpace(viewModel.Vin) ? null : viewModel.Vin.Trim();
        vehicle.LicensePlate = string.IsNullOrWhiteSpace(viewModel.LicensePlate) ? null : viewModel.LicensePlate.Trim();

        try
        {
            await _vehicleRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(viewModel.Vin), "A vehicle with that VIN already exists.");
            return View(viewModel);
        }

        return RedirectToAction(nameof(Details), new { id = vehicle.CustomerId });
    }

    // POST /Customers/DeleteVehicle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVehicle(int id, int customerId)
    {
        var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == id);
        if (vehicle == null)
            return NotFound();

        // Business rule: can't delete a vehicle that has appointment history —
        // remove/cancel those first so the schedule isn't silently orphaned.
        var appointments = await _appointmentRepository.FindByAsync(a => a.VehicleId == id);
        if (appointments.Count > 0)
        {
            var customer = await _customerRepository.GetByIdWithVehiclesAsync(customerId);
            if (customer == null)
                return NotFound();

            var viewModel = BuildDetailsViewModel(customer);
            viewModel.ErrorMessage = "Can't delete this vehicle — it has appointment history on file.";
            return View(nameof(Details), viewModel);
        }

        _vehicleRepository.Delete(vehicle);
        await _vehicleRepository.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = customerId });
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
