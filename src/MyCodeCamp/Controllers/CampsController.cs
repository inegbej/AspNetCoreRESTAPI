using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Filters;
using MyCodeCamp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyCodeCamp.Controllers
{
    [Authorize]
    [EnableCors("AnyGET")]
    [Route("api/[controller]")]
    [ValidateModel]
    public class CampsController : BaseController
    {
        private ILogger<CampsController> _logger;
        private ICampRepository _repo;
        private IMapper _mapper;

        // ctor
        public CampsController(ICampRepository repo, ILogger<CampsController> logger, IMapper mapper)
        {
            _repo = repo;
            _logger = logger;
            _mapper = mapper;       
        }

        [HttpGet("")]   // same as the route at the class level
        public IActionResult Get()
        {
            var camps = _repo.GetAllCamps();
            return Ok(_mapper.Map<IEnumerable<CampModel>>(camps));
        }

        // Use the repo to Get a camp with or without speaker. The Name attribute is for getting a newly inserted Camp, specified in the POST operation
        [HttpGet("{moniker}", Name = "CampGet")]
        public IActionResult Get(string moniker, bool includeSpeakers = false)
        {
            try
            {
                Camp camp = null;

                if (includeSpeakers) camp = _repo.GetCampByMonikerWithSpeakers(moniker);
                else camp = _repo.GetCampByMoniker(moniker);

                if (camp == null) return NotFound($"Camp {moniker} was not found");

                return Ok(_mapper.Map<CampModel>(camp));
            }
            catch
            {

            }
            return BadRequest();
        }

        // get a single camp with no speakers
        //[HttpGet("{id}")]
        //public IActionResult Get(int id)
        //{
        //    try
        //    {
        //        var camp = _repo.GetCamp(id);

        //        if (camp == null) return NotFound($"Camp {id} was not found");

        //        return Ok(camp);
        //    }
        //    catch
        //    {

        //    }
        //    return BadRequest();
        //}

        [EnableCors("Wildermuth")]
        [Authorize(Policy = "SuperUsers")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]CampModel model)
        {
            try
            {
                _logger.LogInformation("++++++++++++ Creating a new Code Camp ++++++++++++ ");

                var camp = _mapper.Map<Camp>(model);  // Pass in a CampModel BUT we want a Camp Entity returned

                _repo.Add(camp);

                if (await _repo.SaveAllAsync())
                {
                    // construct a uri for the newly inserted data
                    var newUri = Url.Link("CampGet", new { moniker = camp.Moniker });
                    return Created(newUri, _mapper.Map<CampModel>(camp));              // Pass in a camp Entity And we want a CampModel returned
                }
                else
                {
                    _logger.LogWarning("+++++++22222222 Could not save Camp to the database ++++++++22222222222");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"++++++++++3333333333333 Threw exception while saving Camp:\n++++++++333333333 {ex}");
            }

            return BadRequest();
        }

        //[HttpPatch]
        [HttpPut("{moniker}")]
        public async Task<IActionResult> Put(string moniker, [FromBody] CampModel model)
        {
            try
            {
                var oldCamp = _repo.GetCampByMoniker(moniker);
                if (oldCamp == null) return NotFound($"Could not found a Camp with an ID of {moniker}");

                // Map the model to the oldcamp - updates
                _mapper.Map(model, oldCamp);

                if (await _repo.SaveAllAsync())  // call save changes
                {
                    return Ok(_mapper.Map<CampModel>(oldCamp));    // map entities to Models/dto
                }
            }
            catch (Exception ex)
            {
            }
            return BadRequest("++++++++++ Couldn't update Camp+++++++++++++++++++++++");
        }
        
        [HttpDelete("{moniker}")]
        public async Task<IActionResult> Delete(string moniker)
        {
            try
            {
                var oldCamp = _repo.GetCampByMoniker(moniker);
                if (oldCamp == null) return NotFound($"Could not found a Camp with an ID of {moniker}");

                _repo.Delete(oldCamp);
                if (await _repo.SaveAllAsync())
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
            }
            return BadRequest("Could not delete camp");
        }

    }
}
