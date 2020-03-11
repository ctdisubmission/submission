using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CandidateProject.ViewModels;

namespace CandidateProject.Models
{
    public interface ICartonService
    {
        void RemoveEquipmentOnCarton(RemoveEquipmentViewModel removeEquipmentViewModel);
    }
}