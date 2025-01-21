using BosnetTest.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BosnetTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public UserController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpGet(Name = "GetUser")]
        public IActionResult Get()
        {
            try
            {
                var data = _databaseService.GetAllData();
                return Ok(data);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while accessing the database.", error = ex.Message });
            }
            //return Enumerable.Range(1, 5).Select(index => new User
            //{
            //    Id = 1,
            //    Name = "Kemal Mahmud",
            //    Address = "Jalan Pasir Impun"
            //})
            //.ToArray();
        }
    }
}
