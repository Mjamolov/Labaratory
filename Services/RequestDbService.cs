using Labaratory.DbContext;
using Labaratory.Models;
using Labaratory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Labaratory.Services
{
    public class RequestDbService : IRequestDbService
    {
        private readonly ApplicationContext? _applicationContext;

        public RequestDbService(ApplicationContext? applicationContext)
        {
            _applicationContext = applicationContext;
        }

        public void AddNewPatient(Patient patient)
        {
            try
            {
                var response = _applicationContext?.Patients?.Add(patient);
                _applicationContext?.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
