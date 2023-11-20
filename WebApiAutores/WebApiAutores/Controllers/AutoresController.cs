using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiAutores.Dtos;
using WebApiAutores.Dtos.Autores;
using WebApiAutores.Dtos.Book;
using WebApiAutores.Entities;
using WebApiAutores.Filters;

namespace WebApiAutores.Controllers
{
    [Route("api/autores")]
    [ApiController]
    [Authorize]
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AutoresController(ApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        //Obtener Autor
        [HttpGet]
        public async Task<ActionResult<ResponseDto<IReadOnlyList<AutorDto>>>> get()
        {
            var autorDb = await _context.Autores.ToListAsync();

            var autoresDto = _mapper.Map<List<AutorDto>>(autorDb);

            return new ResponseDto<IReadOnlyList<AutorDto>>
            {
                Status = true,
                Data = autoresDto
            };
        }


        //Obtener Autor por Id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResponseDto<AutorGetByIdDto>>> GetOneById(int id) 
        {
            var autorDb = await _context.Autores
                .Include(a => a.Books)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (autorDb is null)
            {
                return NotFound(new ResponseDto<AutorDto>
                {
                    Status = false,
                    Message = $"El autor con el Id {id} no fue encontrado"
                });
            }

            var autorDto = _mapper.Map<AutorGetByIdDto>(autorDb);

            return Ok(new ResponseDto<AutorDto>
            {
                Status = true,
                Data = autorDto
            });
        }

        //Crear autor
        [HttpPost]
        public async Task<ActionResult> Post(AutorCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Error en la peticion");
            }

            // Subir imagen a Cloudinary
            var account = new Account(
                "webapiautores", // cloud name
                "657274843415954", // api_key
                "NfVhYQnqgKo-2biOxXigCMH5yx0" // api_secret
            );

            var cloudinary = new Cloudinary(account);

            try
            {
                var uploadParams = new ImageUploadParams()
                {
                    //Ruta local de la imagen
                    File = new FileDescription(dto.ImageSrc),

                    //Identificador único para cada imagen
                    PublicId = $"autor_{Guid.NewGuid()}"
                };

                var uploadResult = cloudinary.Upload(uploadParams);

                var autorDb = _mapper.Map<Autor>(dto);

                _context.Add(autorDb);
                await _context.SaveChangesAsync();

                // Guardar la URL de la imagen de Cloudunary
                autorDb.ImageCloudinary = uploadResult.Url.ToString();


                var autorDto = _mapper.Map<AutorDto>(autorDb);

                return StatusCode(StatusCodes.Status201Created, new ResponseDto<AutorDto>
                {
                    Status = true,
                    Message = "Autor creado correctamente",
                    Data = autorDto
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al subir la imagen a Cloudinary: {ex.Message}");
            }
        }

        //Editar autor
        [HttpPut("{id:int}")] // api/autores/4
        public async Task<IActionResult> Put(int id, AutorUpdateDto dto) 
        {
            var autorDb = await _context.Autores.FirstOrDefaultAsync(a => a.Id == id);
            if (autorDb is null)
            {
                return NotFound(new ResponseDto<AutorDto>
                {
                    Status = false,
                    Message = $"El autor con el Id {id} no fue encontrado"
                });
            }

            // Verifica si es una nueva imagen
            if (!string.IsNullOrEmpty(dto.ImageSrc))
            {
                //Cloudinary
                var account = new Account(
                   "webapiautores", // cloud name
                   "657274843415954", // api_key
                   "NfVhYQnqgKo-2biOxXigCMH5yx0" // api_secret
                );

                var cloudinary = new Cloudinary(account);

                try
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(dto.ImageSrc),
                        PublicId = $"autor_{Guid.NewGuid()}"
                    };
                    var uploadResult = cloudinary.Upload(uploadParams);

                    autorDb.ImageCloudinary = uploadResult.Url.ToString();
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error al subir la imagen a Cloudinary: {ex.Message}");
                }
            }

            //Mapeo de datos
            _mapper.Map<AutorUpdateDto, Autor>(dto, autorDb);

            _context.Update(autorDb);

            await _context.SaveChangesAsync();

            var autorDto = _mapper.Map<AutorDto>(autorDb);

            return Ok(new ResponseDto<AutorDto>
            {
                Status = true,
                Message = "Autor actualizado correctamente",
                Data = autorDto
            });
        }

        //Eliminar Autor
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id) 
        {
            var autor = await _context.Autores.FirstOrDefaultAsync(a => a.Id == id);
            if (autor is null)
            {
                return NotFound(new ResponseDto<AutorDto>
                {
                    Status = false,
                    Message = $"El autor con el Id {id} no fue encontrado"
                });
            }

            //Eliminar Imagen de Cloudinary
            // Verificar si existe una URL de imagen asociada al libro
            if (!string.IsNullOrEmpty(autor.ImageCloudinary))
            {
                var account = new Account(
                    "webapiautores", // cloud name
                    "657274843415954", // api_key
                    "NfVhYQnqgKo-2biOxXigCMH5yx0" // api_secret
                );

                var cloudinary = new Cloudinary(account);

                try
                {
                    // Obtener el publicId de la imagen en Cloudinary a partir de la URL almacenada
                    var publicId = ObtenerPublicIdDesdeURL(autor.ImageCloudinary);

                    // Eliminar la imagen de Cloudinary usando el publicId obtenido
                    var deletionParams = new DeletionParams(publicId);
                    var deletionResult = await cloudinary.DestroyAsync(deletionParams);

                    // Verificar si la eliminación fue exitosa
                    if (deletionResult.Result == "ok")
                    {
                        // Eliminar el libro si la eliminación de la imagen en Cloudinary fue exitosa
                        _context.Remove(autor);
                        await _context.SaveChangesAsync();

                        return Ok(new ResponseDto<string>
                        {
                            Status = true,
                            Message = @"El autor y su fotografia
                            en Cloudinary fueron borrados correctamente"
                        });
                    }
                    else
                    {
                        return BadRequest("Error al eliminar la fotografia en Cloudinary");
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error al eliminar la fotografia en Cloudinary: {ex.Message}");
                }
            }

            //Guardar cambios
            _context.Remove(autor);
            await _context.SaveChangesAsync();

            return Ok(new ResponseDto<string>
            {
                Status = true,
                Message = "autor borrado correctamente"
            });
        }

        //Funciones
        //Obtener PublicId de Url en CloudDinary
        private string ObtenerPublicIdDesdeURL(string imageUrl)
        {
            var uri = new Uri(imageUrl);
            var publicIdWithExtension = Path.GetFileNameWithoutExtension(uri.Segments.LastOrDefault()?.Trim('/'));

            // Obtener publicId sin extensión de archivo
            var publicId = publicIdWithExtension.Substring(publicIdWithExtension.IndexOf('/') + 1); 
            return publicId;
        }
    }
}
