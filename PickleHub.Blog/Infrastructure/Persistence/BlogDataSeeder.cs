using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PickleHub.Blog.Domain.Entities;
using PickleHub.Blog.Domain.Enums;
using PickleHub.Common.ValueObjects;

namespace PickleHub.Blog.Infrastructure.Persistence
{
    public static class BlogDataSeeder
    {
        public static async Task MigrateAndSeedBlogAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogDbContext>>();

            try
            {
                logger.LogInformation("--> [PickleHub.Blog] Applying database migrations if any...");
                await db.Database.MigrateAsync();

                logger.LogInformation("--> [PickleHub.Blog] Synchronizing 4 Selkirk-style blog categories...");

                // 4 Danh mục chuẩn Selkirk
                var catDefs = new[]
                {
                    new
                    {
                        Name = "Sản Phẩm Pickleball",
                        Slug = "san-pham-pickleball",
                        Desc = "Tìm hiểu về vợt, phụ kiện và công nghệ hiệu suất giúp cải thiện lối chơi trên sân.",
                        Order = 1
                    },
                    new
                    {
                        Name = "Kiến Thức Pickleball",
                        Slug = "kien-thuc-pickleball",
                        Desc = "Khóa học và bài học giúp bạn cải thiện từng kỹ năng cụ thể trong pickleball.",
                        Order = 2
                    },
                    new
                    {
                        Name = "Tin Tức Pickleball",
                        Slug = "tin-tuc-pickleball",
                        Desc = "Tin tức và ghi chú từ thế giới pickleball chuyên nghiệp, cùng những câu chuyện từ các thành viên cộng đồng đang góp phần phát triển môn thể thao này.",
                        Order = 3
                    },
                    new
                    {
                        Name = "Cộng Đồng Pickleball",
                        Slug = "cong-dong-pickleball",
                        Desc = "Mỗi môn thể thao đều tạo nên văn hóa riêng. Khám phá lịch sử pickleball, những quy tắc ứng xử không thành văn trên sân và các mẹo phong cách sống pickleball, cả trong lẫn ngoài sân.",
                        Order = 4
                    }
                };

                var existingCategories = await db.Categories.ToListAsync();
                var categoryMap = new Dictionary<string, ContentCategory>();

                foreach (var def in catDefs)
                {
                    var existing = existingCategories.FirstOrDefault(c => c.Slug.Value == def.Slug);
                    if (existing == null)
                    {
                        var newCat = ContentCategory.Create(
                            def.Name,
                            Slug.Create(def.Slug),
                            def.Desc,
                            def.Order
                        );
                        await db.Categories.AddAsync(newCat);
                        categoryMap[def.Slug] = newCat;
                    }
                    else
                    {
                        existing.Update(def.Name, Slug.Create(def.Slug), def.Desc, def.Order);
                        categoryMap[def.Slug] = existing;
                    }
                }

                // Xóa các danh mục cũ nếu không còn dùng
                var activeSlugs = catDefs.Select(d => d.Slug).ToHashSet();
                var obsoleteCategories = existingCategories.Where(c => !activeSlugs.Contains(c.Slug.Value)).ToList();
                if (obsoleteCategories.Any())
                {
                    var fallbackCat = categoryMap["san-pham-pickleball"];
                    var obsoleteIds = obsoleteCategories.Select(c => c.Id).ToList();
                    var postsToReassign = await db.Posts.Where(p => obsoleteIds.Contains(p.CategoryId)).ToListAsync();
                    foreach (var p in postsToReassign)
                    {
                        p.UpdateContent(p.Title, p.Slug, p.Content, fallbackCat.Id, p.Summary, p.SeoTitle, p.SeoDescription);
                    }
                    db.Categories.RemoveRange(obsoleteCategories);
                }

                await db.SaveChangesAsync();

                // Nạp lại danh mục đã lưu vào DB
                var savedCategories = await db.Categories.ToListAsync();
                var catSanPham = savedCategories.First(c => c.Slug.Value == "san-pham-pickleball");
                var catKienThuc = savedCategories.First(c => c.Slug.Value == "kien-thuc-pickleball");
                var catTinTuc = savedCategories.First(c => c.Slug.Value == "tin-tuc-pickleball");
                var catCongDong = savedCategories.First(c => c.Slug.Value == "cong-dong-pickleball");

                var adminAuthorId = Guid.Parse("00000000-0000-0000-0000-000000000001");

                // Seed hoặc Update bài viết mẫu
                var postDefs = new[]
                {
                    new
                    {
                        Title = "Hướng dẫn chọn vợt Pickleball chuẩn nhất cho người mới bắt đầu (2026)",
                        Slug = "huong-dan-chon-vot-pickleball-cho-nguoi-moi-bat-dau",
                        Summary = "Phân tích chi tiết độ dày lõi (13mm vs 16mm), chất liệu mặt Carbon T700 và trọng lượng vợt phù hợp với thể lực người Việt Nam.",
                        CatId = catSanPham.Id,
                        Image = "/images/paddle.png",
                        PublicId = "seed-paddle-1",
                        Content = @"## 1. Tầm quan trọng của việc chọn đúng vợt Pickleball
Khi mới bước chân vào bộ môn Pickleball, việc lựa chọn một cây vợt phù hợp đóng vai trò quyết định đến 70% cảm giác bóng và sự tiến bộ trong kỹ năng của bạn. Một cây vợt quá nặng sẽ dễ gây chấn thương cổ tay (tennis elbow), trong khi vợt quá nhẹ lại khó tạo ra lực đánh uy lực.

## 2. Các yếu tố kỹ thuật cốt lõi cần lưu ý
* **Độ dày lõi vợt (Core Thickness)**:
  * **13mm - 14mm (Power / Tốc độ)**: Cho độ nảy cao, bóng rời mặt vợt nhanh, phù hợp lối đánh tấn công dồn ép đối thủ.
  * **16mm (Control / Kiểm soát)**: Hấp thụ lực tốt, diện tích điểm ngọt (Sweet Spot) lớn, giúp bạn dễ dàng thực hiện các pha dink bóng tinh tế và phòng thủ mềm mại.
* **Chất liệu bề mặt (Face Material)**:
  * **Carbon Toray T700 / Raw Carbon**: Mặt nhám ma sát cao tạo độ xoáy Spin bóng tối đa.
  * **Fiberglass (Sợi thủy tinh)**: Độ đàn hồi linh hoạt, cảm giác đánh đầm tay và bộc phát lực mạnh.
* **Trọng lượng (Weight)**:
  * Người chơi Việt Nam nên bắt đầu với trọng lượng trung bình từ **215g - 230g (7.6 - 8.1 oz)** để vừa đảm bảo tốc độ vung vợt vừa hạn chế mỏi cơ.

## 3. Lời khuyên từ chuyên gia PickleHub
Hãy bắt đầu với cây vợt lõi 16mm mặt Carbon để làm chủ cảm giác kiểm soát bóng trước khi nâng cấp lên các dòng vợt chuyên sâu về lực đẩy."
                    },
                    new
                    {
                        Title = "Top 5 cây vợt Pickleball kiểm soát & tạo xoáy Spin cực hiểm năm 2026",
                        Slug = "top-5-cay-vot-pickleball-kiem-soat-tao-xoay-spin-2026",
                        Summary = "Đánh giá thực tế các dòng vợt được các tay vợt chuyên nghiệp săn đón nhất với công nghệ mặt nhám ma sát cao và USAPA Approved.",
                        CatId = catSanPham.Id,
                        Image = "/images/paddle.png",
                        PublicId = "seed-paddle-2",
                        Content = @"## Đánh giá chi tiết Top 5 siêu phẩm tạo xoáy hàng đầu
Xu hướng thi đấu Pickleball hiện đại đòi hỏi khả năng tạo xoáy Spin cực cao để ép bóng chìm nhanh sát lưới và gây khó khăn cho đối thủ khi trả giao bóng.

### 1. PickleHub Pro Carbon T700 SpinMaster
* **Chất liệu**: Toray T700 Raw Carbon nhập khẩu.
* **Độ dày**: 16mm Polypropylene Honeycomb.
* **Đặc điểm nổi bật**: Độ nhám bề mặt được khắc laser nano, duy trì ma sát bền bỉ gấp 3 lần so với công nghệ phun cát truyền thống.

### 2. Joola Ben Johns Perseus CFS 16
Cây vợt huyền thoại mang lại độ chính xác tuyệt đối trong các pha dink bóng và reset bóng từ khu vực Baseline.

### 3. Selkirk Vanguard Power Air Invikta
Thiết kế khí động học không viền (Edgeless) độc quyền giúp tối ưu hóa tốc độ vung tay và phản xạ nhanh trên lưới.

### 4. CRBN 1X Power Series
Sự kết hợp hoàn hảo giữa sức mạnh bộc phát và cảm giác kiểm soát bóng chắc chắn.

### 5. Franklin Signature Pro Carbon
Mẫu vợt chuẩn thi đấu USAPA với mức giá cực kỳ dễ tiếp cận cho người chơi phong trào chất lượng cao."
                    },
                    new
                    {
                        Title = "Luật chơi Pickleball cơ bản & Chiến thuật đánh đôi dink bóng trên lưới",
                        Slug = "luat-choi-pickleball-va-chien-thuat-danh-doi-dink-bong",
                        Summary = "Tổng hợp quy tắc tính điểm, vùng Non-Volley Zone (Kitchen) và các mẹo kiểm soát nhịp đấu giúp bạn làm chủ trận đấu dễ dàng.",
                        CatId = catKienThuc.Id,
                        Image = "/images/balls.png",
                        PublicId = "seed-balls-1",
                        Content = @"## 1. Quy tắc cốt lõi cần nhớ trong Pickleball
* **Vùng Kitchen (Non-Volley Zone)**: Khu vực 2.13m tính từ lưới. Người chơi không được đứng trong vùng này để đánh bóng trực tiếp trên không (Volley).
* **Quy tắc 2 lần nảy bóng (Two-Bounce Rule)**: Đội giao bóng và đội nhận giao bóng đều phải để bóng nảy 1 lần trên mặt sân trước khi được phép đánh bóng qua lưới.
* **Cách tính điểm đánh đôi**: Điểm số luôn gồm 3 chữ số: *(Điểm đội giao - Điểm đội nhận - Người giao bóng 1 hay 2)*.

## 2. Nghệ thuật Dink bóng - Chìa khóa chiến thắng
Dink là cú đánh nhẹ đưa bóng rơi qua lưới và nằm gọn trong vùng Kitchen của đối phương:
1. Giữ đầu vợt luôn hướng lên trên, thả lỏng cổ tay.
2. Dùng lực nâng nhẹ nhàng từ cẳng tay và khớp vai, không dùng lực cổ tay vẩy bóng.
3. Nhắm bóng vào chân hoặc góc chéo sân để đối thủ không thể đập bóng (Smash).

## 3. Chiến thuật di chuyển đồng bộ
Luôn di chuyển tiến - lùi cùng nhịp với đồng đội tạo thành một 'bức tường chắn' vững chắc trên đường biên Kitchen line."
                    },
                    new
                    {
                        Title = "So sánh bóng Pickleball thi đấu trong nhà (Indoor) và ngoài trời (Outdoor)",
                        Slug = "so-sanh-bong-pickleball-indoor-va-outdoor",
                        Summary = "Sự khác biệt cốt lõi về số lỗ (26 lỗ vs 40 lỗ), trọng lượng, độ nảy và độ bền khi chơi trên các bề mặt sân khác nhau.",
                        CatId = catSanPham.Id,
                        Image = "/images/balls.png",
                        PublicId = "seed-balls-2",
                        Content = @"## Sự khác biệt cốt lõi giữa bóng Indoor và Outdoor

| Tiêu chí | Bóng Ngoài Trời (Outdoor) | Bóng Trong Nhà (Indoor) |
| :--- | :--- | :--- |
| **Số lượng lỗ** | 40 lỗ nhỏ khoan chính xác | 26 lỗ lớn |
| **Độ cứng** | Cứng hơn, nặng hơn để cản gió | Mềm hơn, nảy êm và bám sàn gỗ |
| **Tốc độ bay** | Nhanh, đầm bóng | Vừa phải, dễ kiểm soát nhịp |
| **Độ bền** | Chống nứt vỡ trên bề mặt bê tông | Chống móp méo trên sàn vinyl / gỗ |

## Lựa chọn đúng loại bóng cho điều kiện sân của bạn
* Nếu bạn chơi tại các cụm sân ngoài trời có gió nhẹ hoặc sân bê tông asphalt, bóng 40 lỗ như **Dura Fast 40** hoặc **Franklin X-40** là tiêu chuẩn bắt buộc.
* Đối với các nhà thi đấu có sàn gỗ hoặc thảm thể thao kín gió, hãy chọn bóng 26 lỗ để giảm tiếng ồn và tăng thời lượng bóng bền (Rally)."
                    },
                    new
                    {
                        Title = "Văn hóa ứng xử trên sân Pickleball và những quy tắc bất thành văn",
                        Slug = "van-hoa-ung-xu-tren-san-pickleball-va-quy-tac-bat-thanh-van",
                        Summary = "Khám phá tinh thần Fair-Play, phép lịch sự bắt tay sau trận và phong cách sống văn minh của cộng đồng người chơi Pickleball toàn cầu.",
                        CatId = catCongDong.Id,
                        Image = "/images/paddle.png",
                        PublicId = "seed-community-1",
                        Content = @"## 1. Tinh thần cởi mở và thân thiện
Pickleball phát triển mạnh mẽ nhờ tính cộng đồng cao, nơi mọi người từ người mới chơi đến các tay vợt chuyên nghiệp đều có thể giao lưu học hỏi lẫn nhau.

## 2. Các quy tắc ứng xử cơ bản trên sân
* **Hô điểm to và rõ ràng** trước mỗi lần phát bóng để đối thủ chuẩn bị sẵn sàng.
* **Xác nhận bóng trong/ngoài trung thực**: Hãy luôn tin tưởng góc nhìn của đội đối phương khi bóng rơi sát vạch.
* **Không bước vào sân của người khác** khi trận đấu của họ đang diễn ra pha bóng căng thẳng.
* **Bắt tay hoặc chạm vợt** sau khi kết thúc set đấu để thể hiện sự tôn trọng đối thủ."
                    },
                    new
                    {
                        Title = "Giải vô địch Pickleball Châu Á PPA Tour 2026 chính thức khởi tranh",
                        Slug = "giai-vo-dich-pickleball-chau-a-ppa-tour-2026",
                        Summary = "Quy tụ hơn 500 vận động viên chuyên nghiệp từ 15 quốc gia với tổng giải thưởng lên tới 100.000 USD tại TP. Hồ Chí Minh.",
                        CatId = catTinTuc.Id,
                        Image = "/images/balls.png",
                        PublicId = "seed-news-1",
                        Content = @"## Sự kiện thể thao Pickleball lớn nhất khu vực
PPA Tour Châu Á 2026 đánh dấu bước tiến vượt bậc của phong trào Pickleball tại Việt Nam với sự tham gia của các tay vợt hàng đầu thế giới.

## Các nội dung thi đấu chính
* Đơn nam / Đơn nữ Open Pro
* Đôi nam / Đôi nữ Open Pro
* Đôi nam nữ hỗn hợp (Mixed Doubles)

PickleHub vinh dự là nhà tài trợ thiết bị và bóng thi đấu chính thức cho giải đấu lần này."
                    }
                };

                var existingPosts = await db.Posts.ToListAsync();

                foreach (var pDef in postDefs)
                {
                    var seoTitle = pDef.Title.Length > 65 ? pDef.Title.Substring(0, 65) : pDef.Title;
                    var seoDesc = pDef.Summary != null && pDef.Summary.Length > 155 ? pDef.Summary.Substring(0, 155) : pDef.Summary;

                    var existingPost = existingPosts.FirstOrDefault(p => p.Slug.Value == pDef.Slug);
                    if (existingPost == null)
                    {
                        var newPost = Post.Create(
                            pDef.Title,
                            Slug.Create(pDef.Slug),
                            pDef.Content,
                            pDef.CatId,
                            adminAuthorId,
                            pDef.Summary,
                            seoTitle,
                            seoDesc
                        );
                        newPost.SetCoverImage(pDef.Image, pDef.PublicId);
                        newPost.Publish();
                        await db.Posts.AddAsync(newPost);
                    }
                    else
                    {
                        existingPost.UpdateContent(
                            pDef.Title,
                            existingPost.Slug,
                            pDef.Content,
                            pDef.CatId,
                            pDef.Summary,
                            seoTitle,
                            seoDesc
                        );
                    }
                }

                await db.SaveChangesAsync();
                logger.LogInformation("--> [PickleHub.Blog] Synchronized 4 Selkirk-style categories & blog posts successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "--> [PickleHub.Blog] Error during category/post seeding: {Message}", ex.Message);
            }
        }
    }
}
