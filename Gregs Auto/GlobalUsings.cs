// The web project orchestrates every module, so it imports all of them rather
// than repeating the same four usings in each controller.
//
// This is deliberately only done here and in the test project. Inside the
// Domain, modules import each other explicitly — that's what makes an
// unintended dependency between modules visible in a diff.
global using Gregs_Auto.Domain.Scheduling;
global using Gregs_Auto.Domain.Catalog;
global using Gregs_Auto.Domain.Identity;
global using Gregs_Auto.Domain.Shared;
