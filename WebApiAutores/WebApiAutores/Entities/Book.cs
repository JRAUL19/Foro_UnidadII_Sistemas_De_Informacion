
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiAutores.Entities
{
    [Table("books", Schema = "transaccional")]
    public class Book
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("isbn")]
        [StringLength(50)]
        [Required]
        public string ISBN { get; set; }

        [Column("title")]
        [StringLength(50)]
        [Required]
        public string Title { get; set; }

        [Column("publication_date")]
        [DataType(DataType.Date)]
        public DateTime PublicationDate { get; set; }

        [Column("autor_id")]
        public int AutorId { get; set; }

        [Column("valoracion")]
        
        public double Valoracion {  get; set; }

        [Column("imagen_subida")] // imagen subida por el usuario (debe ser imagen guarda en local)
        [StringLength(255)] 
        public string ImagenSubida { get; set; }

        [Column("Imagen_En_Cloudinary")] // imagen guarda en cloudinary
        [StringLength(255)] 
        public string ImagenEnCloudinary { get; set; }

        [ForeignKey(nameof(AutorId))]
        public virtual Autor Autor { get; set; }

    }
}
