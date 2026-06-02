using CoworkingSpace.BLL;
using CoworkingSpace.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingSpace.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceSpacesController : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var workSpaces = await clsWorkspaceSpaces.GetAllWorkspaceSpaces();
                if (workSpaces == null || workSpaces.Count == 0)
                    return NotFound("No workspace spaces.");
                return Ok(workSpaces);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: During get the workspace spaces details.");

            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid workspace space ID.");
                }


                var workspaceSpace = await clsWorkspaceSpaces.Find(id);
                if (workspaceSpace == null)
                {
                    return NotFound();
                }
                return Ok(workspaceSpace);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: During get the workspace space details.");

            }

        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] workspaceSpaceModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("Workspace space data is required.");
                }
                clsWorkspaceSpaces workspaceSpace = new clsWorkspaceSpaces
                {
                    Title = model.Title,
                    Description = model.Description,
                    SpaceType = model.SpaceType,
                    PricePerHour = model.PricePerHour,
                    PricePerDay = model.PricePerDay,
                    Capacity = model.Capacity,
                    IsAvailable = model.IsAvailable
                };
                bool isSaved = await workspaceSpace.Save();
                if (!isSaved)
                {
                    return StatusCode(500, "An error occurred while saving the workspace space details.");
                }
                return Ok("Workspace space added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: During add the workspace space details.");
            }

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid workspace space ID.");
                }
                bool isDeleted = await clsWorkspaceSpaces.Delete(id);
                if (!isDeleted)
                {
                    return StatusCode(500, "An error occurred while deleting the workspace space.");
                }
                return Ok("Workspace space deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: During delete the workspace space details.");
            }

        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] workspaceSpaceModel model)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid workspace space ID.");
                }
                if (model == null)
                {
                    return BadRequest("Workspace space data is required.");
                }
                clsWorkspaceSpaces workspaceSpace = new clsWorkspaceSpaces
                {
                    Id = id,
                    Title = model.Title,
                    Description = model.Description,
                    SpaceType = model.SpaceType,
                    PricePerHour = model.PricePerHour,
                    PricePerDay = model.PricePerDay,
                    Capacity = model.Capacity,
                    IsAvailable = model.IsAvailable
                };
                bool isUpdated = await workspaceSpace.Save();
                if (!isUpdated)
                {
                    return StatusCode(500, "An error occurred while updating the workspace space details.");
                }
                return Ok("Workspace space updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: During update the workspace space details.");
            }
        }

    }
}