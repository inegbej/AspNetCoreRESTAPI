using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCodeCamp.Controllers;
using MyCodeCamp.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// NOTE: This is a duplicat url resolver for Speaker. But we could easily have create a abse url resolver so that it can be inherited by camp, speaker etc
namespace MyCodeCamp.Models
{
    public class SpeakerUrlResolver : IValueResolver<Speaker, SpeakerModel, string>
    {
        private IHttpContextAccessor _httpContextAccessor;

        public SpeakerUrlResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string Resolve(Speaker source, SpeakerModel destination, string destMember, ResolutionContext context)
        {
            var url = (IUrlHelper)_httpContextAccessor.HttpContext.Items[BaseController.URLHELPER];
            return url.Link("SpeakerGet", new { moniker = source.Camp.Moniker, source.Id });     // constructing a url for speaker

        }

    }
}
