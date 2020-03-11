using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CandidateProject.ViewModels;

namespace CandidateProject.Models
{
    public class CartonService : ICartonService
    {
        private CartonContext db;

        // DM: Don't really have time to setup full DI, but doing constructor injection can substitute for demo
        // I only moved one method but the idea is to move a lot of the business logic out of the controller into a more accessible class
        // so that it can be reused. 
        public CartonService()
        {
            db = new CartonContext();
        }

        public CartonService(CartonContext db)
        {
            this.db = db;
        }

        public void RemoveEquipmentOnCarton(RemoveEquipmentViewModel removeEquipmentViewModel)
        {
            var cartonDetailToDelete = db.CartonDetails.SingleOrDefault(cd => cd.CartonId == removeEquipmentViewModel.CartonId && cd.EquipmentId == removeEquipmentViewModel.EquipmentId);
            if (cartonDetailToDelete != null)
            {
                db.CartonDetails.Remove(cartonDetailToDelete);
                db.SaveChanges();
            }
        }
    }
}