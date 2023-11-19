using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiAutores.Dtos;
using WebApiAutores.Dtos.Book;
using WebApiAutores.Entities;
using WebApiAutores.Services;

namespace WebApiAutores.Controllers
{
    [Route("api/books")]
    [ApiController]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public BooksController(ApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<IReadOnlyList<BookDto>>>> get()
        {
            var booksDb = await _context.Books
                .Include(b => b.Autor)
                .ToListAsync();

            var booksDto = _mapper.Map<List<BookDto>>(booksDb);

            return new ResponseDto<IReadOnlyList<BookDto>>
            {
                Status = true,
                Data = booksDto
            };
        }

        [HttpGet("{id:Guid}")] // api/books/9E343657-45E1-4268-0F14-08DBCA004D0A
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<BookDto>>> Get(Guid id)
        {
            var bookDb = await _context.Books
                .Include(b => b.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (bookDb is null)
            {
                return NotFound(new ResponseDto<BookDto>
                {
                    Status = false,
                    Message = $"El Libro con el Id {id} no fue encontrado"
                });
            }

            var bookDto = _mapper.Map<BookDto>(bookDb);

            return Ok(new ResponseDto<BookDto>
            {
                Status = true,
                Data = bookDto
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Post(BookCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Error en la petición");
            }

            var autorExiste = await _context.Autores.AnyAsync(x => x.Id == dto.AutorId);

            if (!autorExiste)
            {
                return NotFound(new ResponseDto<BookDto>
                {
                    Status = false,
                    Message = $"No existe el autor {dto.AutorId}",
                });
            }
            //////////////////////////////////////////////////////
            // Subir imagen a Cloudinary
            var account = new Account(
                "dxc3qadsk", // cloud name
                "783854393448399", // api_key
                "DP-nz6IpqPZatXMgnsivon0Rj2k" // api_secret
            );

            var cloudinary = new Cloudinary(account);

            try
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(dto.ImagenSubida), // Ruta local de tu imagen a subir
                    PublicId = $"book_{Guid.NewGuid()}" // Utilizar un identificador único para cada imagen
                };
                var uploadResult = cloudinary.Upload(uploadParams);

                var book = _mapper.Map<Book>(dto);

                book.ImagenEnCloudinary = uploadResult.Url.ToString(); // Guardar la URL de la imagen de Cloudunary

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                var bookDto = _mapper.Map<BookDto>(book);

                return StatusCode(StatusCodes.Status201Created, new ResponseDto<BookDto>
                {
                    Status = true,
                    Message = "Libro creado correctamente",
                    Data = bookDto
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al subir la imagen a Cloudinary: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<BookDto>>> Put(BookUpdateDto dto, Guid id)
        {
            var bookDb = await _context.Books.FirstOrDefaultAsync(x => x.Id == id);

            if (bookDb is null)
            {
                return NotFound(new ResponseDto<BookDto>
                {
                    Status = false,
                    Message = $"No existe el libro: {id}"
                });
            }

            var autorExiste = await _context.Autores.AnyAsync(x => x.Id == dto.AutorId);

            if (!autorExiste)
            {
                return NotFound(new ResponseDto<BookDto>
                {
                    Status = false,
                    Message = $"No existe el autor: {dto.AutorId}"
                });
            }

            // Verifica si se proporciono una nueva imagen
            if (!string.IsNullOrEmpty(dto.ImagenSubida))
            {
                //Cloudinary
                var account = new Account(
                    "dxc3qadsk", // cloud name
                    "783854393448399", // api_key
                    "DP-nz6IpqPZatXMgnsivon0Rj2k" // api_secret
                );

                var cloudinary = new Cloudinary(account);

                try
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(dto.ImagenSubida),
                        PublicId = $"book_{Guid.NewGuid()}" 
                    };
                    var uploadResult = cloudinary.Upload(uploadParams);

                    bookDb.ImagenEnCloudinary = uploadResult.Url.ToString();
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error al subir la imagen a Cloudinary: {ex.Message}");
                }
            }

            // Actualizar otros campos del libro
            _mapper.Map<BookUpdateDto, Book>(dto, bookDb);

            _context.Update(bookDb);

            await _context.SaveChangesAsync();

            var bookDto = _mapper.Map<BookDto>(bookDb);
            return Ok(new ResponseDto<BookDto>
            {
                Status = true,
                Message = "Libro actualizado correctamente",
                Data = bookDto
            });
        }


        [HttpDelete("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseDto<string>>> Delete(Guid id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(x => x.Id == id);

            if (book == null)
            {
                return NotFound(new ResponseDto<BookDto>
                {
                    Status = false,
                    Message = $"No existe el libro: {id}"
                });
            }

            // Verificar si existe una URL de imagen asociada al libro
            if (!string.IsNullOrEmpty(book.ImagenEnCloudinary))
            {
                var account = new Account(
                    "dxc3qadsk", // cloud name
                    "783854393448399", // api_key
                    "DP-nz6IpqPZatXMgnsivon0Rj2k" // api_secret
                );

                var cloudinary = new Cloudinary(account);

                try
                {
                    // Obtener el publicId de la imagen en Cloudinary a partir de la URL almacenada
                    var publicId = ObtenerPublicIdDesdeURL(book.ImagenEnCloudinary);

                    // Eliminar la imagen de Cloudinary usando el publicId obtenido
                    var deletionParams = new DeletionParams(publicId);
                    var deletionResult = await cloudinary.DestroyAsync(deletionParams);

                    // Verificar si la eliminación fue exitosa
                    if (deletionResult.Result == "ok")
                    {
                        // Eliminar el libro si la eliminación de la imagen en Cloudinary fue exitosa
                        _context.Remove(book);
                        await _context.SaveChangesAsync();

                        return Ok(new ResponseDto<string>
                        {
                            Status = true,
                            Message = "Libro y su portada en Cloudinary borrados correctamente"
                        });
                    }
                    else
                    {
                        return BadRequest("Error al eliminar la portada en Cloudinary");
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error al eliminar la portada en Cloudinary: {ex.Message}");
                }
            }

            _context.Remove(book);
            await _context.SaveChangesAsync();

            return Ok(new ResponseDto<string>
            {
                Status = true,
                Message = "Libro borrado correctamente"
            });
        }

        // obtener el PublicId desde la URL de Cloudinary
        private string ObtenerPublicIdDesdeURL(string imageUrl)
        {
            var uri = new Uri(imageUrl);
            var publicIdWithExtension = Path.GetFileNameWithoutExtension(uri.Segments.LastOrDefault()?.Trim('/'));
            var publicId = publicIdWithExtension.Substring(publicIdWithExtension.IndexOf('/') + 1); // Obtener solo el publicId sin la extensión del archivo
            return publicId;
        }

    }
}
