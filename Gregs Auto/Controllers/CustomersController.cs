using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

// Staff-only: managing customer/vehicle records isn't something a walk-in
// visitor should be able to do from the public site.
//
// The controller-wide policy is the read floor — every staff role can look at
// customer records. Anything that writes carries ManageCustomers on top, which
// excludes Technicians.
[Authorize(Policy = Policies.ViewCustomers)]
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

        var viewModel = new CustomerListViewModel
        {
            SuccessMessage = TempData["CustomerSuccess"] as string,
            Customers = customers
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.FullName)
                .Select(c => new CustomerRowViewModel
                {
                    Id = c.CustomerId,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone,
                    // Archived cars aren't part of what the shop still looks after.
                    VehicleCount = c.Vehicles.Count(v => v.IsActive),
                    IsActive = c.IsActive
                }).ToList()
        };

        return View(viewModel);
    }

    // GET /Customers/Create
    [Authorize(Policy = Policies.ManageCustomers)]
    public IActionResult Create()
    {
        return View(new CreateCustomerViewModel());
    }

    // POST /Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageCustomers)]
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
    [Authorize(Policy = Policies.ManageCustomers)]
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
    [Authorize(Policy = Policies.ManageCustomers)]
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

    // POST /Customers/SetActive — archive or restore.
    //
    // Customers are archived rather than deleted. A deleted customer takes their
    // vehicles and every job ever done on them with it, which is the opposite of
    // what a shop wants from a service-history system. Archiving hides them from
    // the working list and leaves the history intact.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        var customer = await _customerRepository.GetByIdWithVehiclesAsync(id);
        if (customer == null)
            return NotFound();

        customer.IsActive = isActive;

        // Their cars go with them — an archived customer's vehicles shouldn't
        // keep turning up on the booking form.
        foreach (var vehicle in customer.Vehicles)
            vehicle.IsActive = isActive;

        await _customerRepository.SaveChangesAsync();

        TempData["CustomerSuccess"] = isActive
            ? $"{customer.FullName} is back on the active list."
            : $"{customer.FullName} archived. Their service history is still on file.";

        return RedirectToAction(nameof(Index));
    }

    // GET /Customers/EditVehicle/{id}
    [Authorize(Policy = Policies.ManageCustomers)]
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
    [Authorize(Policy = Policies.ManageCustomers)]
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
        vehicle.Vin = string.IsNullOrWhiteSpace(viewModel.Vin) ? null : viewModel.Vin.Trim().ToUpperInvariant();
        vehicle.LicensePlate = string.IsNullOrWhiteSpace(viewModel.LicensePlate) ? null : viewModel.LicensePlate.Trim().ToUpperInvariant();

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

    // POST /Customers/SetVehicleActive — archive or restore a vehicle.
    //
    // A car that's been sold or written off stops being bookable but keeps its
    // service history, which is the whole point of recording it. Deleting it
    // would throw away the record of work that actually happened.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> SetVehicleActive(int id, int customerId, bool isActive)
    {
        var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == id);
        if (vehicle == null)
            return NotFound();

        vehicle.IsActive = isActive;
        await _vehicleRepository.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = customerId });
    }

    // POST /Customers/AddVehicle
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> AddVehicle(CreateVehicleViewModel newVehicle)
    {
        var customer = await _customerRepository.GetByIdWithVehiclesAsync(newVehicle.CustomerId);
        if (customer == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            var viewModel = BuildDetailsViewModel(customer);
            viewModel.NewVehicle = newVehicle;
            return View(nameof(Details), viewModel);
        }

        var vehicle = new Vehicle
        {
            CustomerId = newVehicle.CustomerId,
            Make = newVehicle.Make.Trim(),
            Model = newVehicle.Model.Trim(),
            Year = newVehicle.Year,
            Vin = string.IsNullOrWhiteSpace(newVehicle.Vin) ? null : newVehicle.Vin.Trim().ToUpperInvariant(),
            LicensePlate = string.IsNullOrWhiteSpace(newVehicle.LicensePlate) ? null : newVehicle.LicensePlate.Trim().ToUpperInvariant(),
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
            viewModel.NewVehicle = newVehicle;
            viewModel.ErrorMessage = "A vehicle with that VIN already exists.";
            return View(nameof(Details), viewModel);
        }

        return RedirectToAction(nameof(Details), new { id = newVehicle.CustomerId });
    }

    private static CustomerDetailsViewModel BuildDetailsViewModel(Customer customer)
    {
        return new CustomerDetailsViewModel
        {
            CustomerId = customer.CustomerId,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            IsActive = customer.IsActive,
            Vehicles = customer.Vehicles
                .OrderByDescending(v => v.IsActive)
                .ThenByDescending(v => v.Year)
                .Select(v => new VehicleRowViewModel
                {
                    Id = v.VehicleId,
                    Year = v.Year,
                    Make = v.Make,
                    Model = v.Model,
                    Vin = v.Vin,
                    LicensePlate = v.LicensePlate,
                    IsActive = v.IsActive
                }).ToList(),
            NewVehicle = new CreateVehicleViewModel { CustomerId = customer.CustomerId }
        };
    }
}
