/* Association in API - Speaker is a child of Camps */

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Route("api/camps/{moniker}/speakers")]
    [ValidateModel]
    public class SpeakersController : BaseController
    {
        protected ILogger<SpeakersController> _logger;
        protected IMapper _mapper;
        protected ICampRepository _repository;
        private UserManager<CampUser> _userMgr;

        public SpeakersController(ICampRepository repository, ILogger<SpeakersController> logger, IMapper mapper, UserManager<CampUser> userMgr)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
            _userMgr = userMgr;
        }

        // Retrieve all Speakers
        [HttpGet]
        public IActionResult Get(string moniker, bool includeTalks = false)
        {
            var speakers = includeTalks ? _repository.GetSpeakersByMonikerWithTalks(moniker) : _repository.GetSpeakersByMoniker(moniker);
            //var speakers = _repository.GetSpeakersByMoniker(moniker);

            return Ok(_mapper.Map<IEnumerable<SpeakerModel>>(speakers));
            //return Ok(speakers);
        }

        // Retrieve a single speaker. With a named route called "SpeakerGet"
        [HttpGet("{id}", Name = "SpeakerGet")]
        public IActionResult Get(string moniker, int id, bool includesTalks = false)
        {
            var speaker = includesTalks ? _repository.GetSpeakerWithTalks(id) : _repository.GetSpeaker(id);
            if (speaker == null) return NotFound();

            // check that the speaker is part of the camp for that moniker
            if (speaker.Camp.Moniker != moniker) return BadRequest("Speaker not in specified camp");

            return Ok(_mapper.Map<SpeakerModel>(speaker));

            //return Ok(speaker);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Post(string moniker, [FromBody] SpeakerModel model)   
        {
            try
            {                
                var camp = _repository.GetCampByMoniker(moniker);            // get the camp
                if (camp == null) return BadRequest("Could not find camp");

                var speaker = _mapper.Map<Speaker>(model);   // get the speaker. convert the passed in model to our Speaker Entities using automapper
                speaker.Camp = camp;                         // assign camp to the speaker - relationship

                // Authentication
                var campUser = await _userMgr.FindByNameAsync(this.User.Identity.Name);  // find the camp user and return it. Ensured that the user logged in is the same user we have
                if (campUser != null)
                {
                    speaker.User = campUser;

                    _repository.Add(speaker);                    //  add speaker to repository

                    if (await _repository.SaveAllAsync())        // save to database
                    {
                        var url = Url.Link("SpeakGet", new { moniker = camp.Moniker, id = speaker.Id }); // generate a url for the new object
                        return Created(url, _mapper.Map<SpeakerModel>(speaker));                         // new speaker is created
                    }
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError($"++++++++++++++++++++ Exception thrown while adding speaker +++++++++++++++++++++++++ {ex}");
            }
            return BadRequest("Could not add new speaker");
        }
        
        [HttpPut("{id}")]        
        public async Task<IActionResult> Put(string moniker, int id, [FromBody] SpeakerModel model)
        {
            try
            {
                var speaker = _repository.GetSpeaker(id);     // get existing speaker
                if (speaker == null) return NotFound();
                // check that the speaker is part of a moniker/camp
                if (speaker.Camp.Moniker != moniker) return BadRequest("Speaker and camp do not match");

                // Ensure that the user logged in, is the same user we have in the system
                if (speaker.User.UserName == this.User.Identity.Name) return Forbid();

                _mapper.Map(model, speaker);    // map source to destination (model to speaker)

                if (await _repository.SaveAllAsync())
                {
                    return Ok(_mapper.Map<SpeakerModel>(speaker));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"+++++++++++++++++++++ Exception thrown while updating speaker +++++++++++++++++++++++ {ex}");
            }
            return BadRequest("Could not update speaker");
        }

        [Authorize]
        [HttpDelete("{id}")]        
        public async Task<IActionResult> Delete(string moniker, int id)
        {
            try
            {
                var speaker = _repository.GetSpeaker(id);           // get existing speaker
                if (speaker == null) return NotFound();
                // check that the speaker is part of a moniker/camp
                if (speaker.Camp.Moniker != moniker) return BadRequest("Speaker and camp do not match");

                // Ensure that the user logged in, is the same user we have in the system
                if (speaker.User.UserName == this.User.Identity.Name) return Forbid();

                _repository.Delete(speaker);

                if (await _repository.SaveAllAsync())
                {
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"+++++++++++++++++++++Exception thrown while deleting speaker:+++++++++++++++++++++ {ex}");
            }
            return BadRequest("Could not delete speaker");
        }

    }
}
