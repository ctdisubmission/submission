using CandidateProject.EntityModels;
using CandidateProject.ViewModels;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace CandidateProject.Controllers
{
    public class CartonController : Controller
    {
        private CartonContext db = new CartonContext();

        // GET: Carton
        public ActionResult Index()
        {
            var cartons = db.Cartons
                .Include(c => c.CartonDetails)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber,
                    EquipmentCount = c.CartonDetails.Count()
                })
                .ToList();

            return View(cartons);
        }

        // GET: Carton/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // GET: Carton/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Carton/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,CartonNumber")] Carton carton)
        {
            if (ModelState.IsValid)
            {
                db.Cartons.Add(carton);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(carton);
        }

        // GET: Carton/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // POST: Carton/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,CartonNumber")] CartonViewModel cartonViewModel)
        {
            if (ModelState.IsValid)
            {
                var carton = db.Cartons.Find(cartonViewModel.Id);
                carton.CartonNumber = cartonViewModel.CartonNumber;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(cartonViewModel);
        }

        // GET: Carton/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Carton carton = db.Cartons.Find(id);
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // POST: Carton/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Carton carton = db.Cartons.Find(id);

            if (carton != null)
            {
                // DM: this is a different pattern then is used else where (existing code uses include and where instead of Find)
                // I believe Find can have some small performance gains due to caching so I'll leave it here but this would be 
                // an item up for team review to determine a preferred pattern for consistency.
                db.Entry(carton).Collection(c => c.CartonDetails).Load();
                db.CartonDetails.RemoveRange(carton.CartonDetails);
                db.Cartons.Remove(carton);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult AddEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var carton = db.Cartons
                .Include(c => c.CartonDetails)
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id,
                    CurrentEquipmentCount = c.CartonDetails.Count()
                })
                .SingleOrDefault();

            if (carton == null)
            {
                return HttpNotFound();
            }

            var availableEquipment = db.Equipments
                .Where(e => !db.CartonDetails.Select(cd => cd.EquipmentId).Contains(e.Id))
                .Select(e => new EquipmentViewModel()
                {
                    Id = e.Id,
                    ModelType = e.ModelType.TypeName,
                    SerialNumber = e.SerialNumber
                })
                .ToList();

            carton.Equipment = availableEquipment;
            return View(carton);
        }

        public ActionResult AddEquipmentToCarton([Bind(Include = "CartonId,EquipmentId")] AddEquipmentViewModel addEquipmentViewModel)
        {
            if (ModelState.IsValid)
            {
                var carton = db.Cartons
                    .Include(c => c.CartonDetails)
                    .Where(c => c.Id == addEquipmentViewModel.CartonId)
                    .SingleOrDefault();
                if (carton == null)
                {
                    return HttpNotFound();
                }

                if (carton.CartonDetails.Count() >= 10) // DM: hardcoded 10, would make more sense to put this in the DB as a config option of the carton, but trying to keep this to the 1 hour mark. 
                                                        // You could then use that in the config value to set the MaxCurrentEquipmentCount I added in the CartonDetailsViewModel.
                {
                    TempData["Message"] = "Your equipment was not added because the carton already has the maximum amount of items.";
                    return RedirectToAction("AddEquipment", new { id = addEquipmentViewModel.CartonId });
                }

                var equipment = db.Equipments
                    .Where(e => e.Id == addEquipmentViewModel.EquipmentId)
                    .SingleOrDefault();
                if (equipment == null)
                {
                    return HttpNotFound();
                }

                var assignedEquipement = db.CartonDetails
                    .Where(cd => cd.EquipmentId == addEquipmentViewModel.EquipmentId)
                    .SingleOrDefault();
                if (assignedEquipement != null)
                {
                    TempData["Message"] = "Your equipment has already been added to another carton.";
                    return RedirectToAction("AddEquipment", new { id = addEquipmentViewModel.CartonId });
                }

                var detail = new CartonDetail()
                {
                    Carton = carton,
                    Equipment = equipment
                };

                carton.CartonDetails.Add(detail);
                db.SaveChanges();
            }
            return RedirectToAction("AddEquipment", new { id = addEquipmentViewModel.CartonId });
        }

        public ActionResult RemoveAllEquipmentFromCarton(int? cartonId)
        {
            if (cartonId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var carton = db.Cartons.Find(cartonId);

            if (carton != null)
            {
                db.Entry(carton).Collection(c => c.CartonDetails).Load();
                db.CartonDetails.RemoveRange(carton.CartonDetails);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // DM: Looks like most of the equipment interaction are done through gets currently. Given the desired timeframe of 30-45 mins I'm not going to change these to posts and add antiforgery etc, but it is something that
        // should be looked at
        public ActionResult ViewCartonEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id,
                    Equipment = c.CartonDetails
                        .Select(cd => new EquipmentViewModel()
                        {
                            Id = cd.EquipmentId,
                            ModelType = cd.Equipment.ModelType.TypeName,
                            SerialNumber = cd.Equipment.SerialNumber
                        })
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        public ActionResult RemoveEquipmentOnCarton([Bind(Include = "CartonId,EquipmentId")] RemoveEquipmentViewModel removeEquipmentViewModel)
        {
            if (ModelState.IsValid)
            {
                // DM: I typically either inject or wrap my contexts directly in a using statement where I need them, so I'll do it here for an example.
                // I realize the db variable is available and should be used and if the team prefered that method that's not a problem for me especially in a web site where the life of a call is well defined
                using (var context = new CartonContext())
                {
                    var cartonDetailToDelete = context.CartonDetails.SingleOrDefault(cd => cd.CartonId == removeEquipmentViewModel.CartonId && cd.EquipmentId == removeEquipmentViewModel.EquipmentId);
                    if (cartonDetailToDelete != null)
                    {
                        context.CartonDetails.Remove(cartonDetailToDelete);
                        context.SaveChanges();
                    }
                }

            }
            return RedirectToAction("ViewCartonEquipment", new { id = removeEquipmentViewModel.CartonId });
        }
    }
}
