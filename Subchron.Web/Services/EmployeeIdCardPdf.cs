using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Subchron.Web.Services;

public static class EmployeeIdCardPdf
{
    private const float ModalW = 320f;
    private const float ModalH = 500f;

    private const float CardW = 250f;
    private const float CardH = 391f; // 500 * (250/320)
    private const float CardCornerRadius = 16f;

    private static readonly float S = CardW / ModalW;

    private static readonly string Navy = "#002d5b";
    private static readonly string MidBlue = "#005ba3";
    private static readonly string BrightBlue = "#00a9e0";
    private static readonly string DarkText = "#1e293b";
    private static readonly string MutedText = "#64748b";

    /// <summary>
    /// Generates a PDF from two card images (front and back) captured from the modal so the output matches the UI exactly.
    /// CardW/CardH match modal card aspect ratio (320/500); FitArea() preserves image aspect so nothing is stretched.
    /// </summary>
    public static byte[] GenerateFromImages(byte[] frontImageBytes, byte[] backImageBytes)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(24);
                p.PageColor(Colors.White);

                p.Content().Row(row =>
                {
                    row.Spacing(20);

                    row.ConstantItem(CardW).Height(CardH)
                        .Element(c => CardShell(c, x => x.Image(frontImageBytes).FitArea()));

                    row.ConstantItem(CardW).Height(CardH)
                        .Element(c => CardShell(c, x => x.Image(backImageBytes).FitArea()));
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateBatchFromImages(IReadOnlyList<CardImagePair> cards)
    {
        if (cards == null || cards.Count == 0)
            throw new ArgumentException("No cards provided.", nameof(cards));

        QuestPDF.Settings.License = LicenseType.Community;

        var batches = cards.Chunk(2).Select(chunk => chunk.ToList()).ToList();

        return Document.Create(container =>
        {
            foreach (var batch in batches)
            {
                var localBatch = batch;
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.PageColor(Colors.White);

                    page.Content().Column(col =>
                    {
                        col.Spacing(20);
                        col.Item().Element(c => RenderBatchRow(c, localBatch, true));
                        col.Item().Element(c => RenderBatchRow(c, localBatch, false));
                    });
                });
            }
        }).GeneratePdf();
    }

    private static void RenderBatchRow(IContainer container, IReadOnlyList<CardImagePair> batch, bool frontSide)
    {
        container.Row(row =>
        {
            row.Spacing(20);
            foreach (var card in batch)
            {
                var bytes = frontSide ? card.Front : card.Back;
                row.ConstantItem(CardW).Height(CardH)
                    .Element(c => CardShell(c, x => x.Image(bytes).FitArea()));
            }

            for (var i = batch.Count; i < 2; i++)
            {
                row.ConstantItem(CardW).Height(CardH)
                    .Element(c => CardShell(c, _ => { }));
            }
        });
    }

    public static byte[] Generate(EmployeeIdCardData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(24);
                p.PageColor(Colors.White);

                p.Content().Row(row =>
                {
                    row.Spacing(20);

                    row.ConstantItem(CardW).Height(CardH)
                        .Element(c => CardShell(c, x => ComposeFront(x, data)));

                    row.ConstantItem(CardW).Height(CardH)
                        .Element(c => CardShell(c, x => ComposeBack(x, data)));
                });
            });
        }).GeneratePdf();
    }

    private static void CardShell(IContainer container, Action<IContainer> compose)
    {
        // Older QuestPDF: CornerRadius(…) is the safe API.
        // In most versions it also clips children to rounded corners.
        container
            .Background(Colors.White)
            .CornerRadius(CardCornerRadius)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten3)
            .Element(compose);
    }

    // ---------------- FRONT ----------------

    private static void ComposeFront(IContainer container, EmployeeIdCardData data)
    {
        var headerH = 180f * S;
        var footerH = 32f * S;
        var photoSize = 140f * S;
        var photoTop = 100f * S;

        var bodyPadX = 32f * S;
        var mt20 = 90f * S;
        var fieldsMt = 34f * S;

        container.Layers(layers =>
        {
            layers.PrimaryLayer().Column(col =>
            {
                col.Item().Height(headerH).Element(c => GeoHeader(c, data));

                col.Item().PaddingTop(mt20).PaddingHorizontal(bodyPadX).Column(body =>
                {
                    body.Item().AlignCenter()
                        .Text((data.FullName ?? "—").ToUpperInvariant())
                        .FontSize(24f * S)
                        .Bold()
                        .FontColor(Navy)
                        .LineHeight(1.0f);

                    body.Item().AlignCenter().PaddingTop(4f * S)
                        .Text((data.Role ?? "—").ToUpperInvariant())
                        .FontSize(12f * S)
                        .Bold()
                        .FontColor(BrightBlue)
                        .LetterSpacing(1.5f * S);

                    body.Item().PaddingTop(fieldsMt).Column(fields =>
                    {
                        FieldBlock(fields, "Email Address", data.Email, BrightBlue);
                        FieldBlock(fields, "Contact No.", data.PhoneFormatted, Colors.Grey.Lighten3);
                        FieldBlock(fields, "Address", data.Address, Colors.Grey.Lighten3);
                    });
                });

                col.Item().Height(footerH)
                    .Background(Navy)
                    .CornerRadius(10f * S);
            });

            layers.Layer().Element(layer =>
            {
                layer.TranslateY(photoTop)
                    .AlignCenter()
                    .Width(photoSize)
                    .Height(photoSize)
                    .Background("#f8fafc")
                    .CornerRadius(photoSize / 2f)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten3)
                    .Element(photo =>
                    {
                        if (data.PhotoBytes is { Length: > 0 })
                            photo.Image(data.PhotoBytes).FitArea();
                        else
                            photo.AlignCenter().AlignMiddle()
                                .Text("PHOTO")
                                .FontSize(10f * S)
                                .FontColor(MutedText);
                    });
            });
        });
    }

    private static void GeoHeader(IContainer container, EmployeeIdCardData data)
    {
        var headerH = 180f * S;

        var o1H = 100f * S;
        var o1W = CardW * 1.40f;
        var o1X = -0.20f * CardW;
        var o1Top = headerH - o1H + (40f * S);

        var o2H = 80f * S;
        var o2W = CardW * 1.40f;
        var o2X = -0.20f * CardW;
        var o2Top = headerH - o2H + (20f * S);

        var owH = 80f * S;
        var owW = CardW * 1.20f;
        var owX = -0.10f * CardW;
        var owTop = headerH - owH + (50f * S);

        container.Background(Navy).Layers(l =>
        {
            l.PrimaryLayer().Element(_ => { });

            l.Layer().Element(x =>
                x.TranslateX(o1X).TranslateY(o1Top)
                 .Width(o1W).Height(o1H)
                 .Rotate(-10)
                 .Background(MidBlue));

            l.Layer().Element(x =>
                x.TranslateX(o2X).TranslateY(o2Top)
                 .Width(o2W).Height(o2H)
                 .Rotate(10)
                 .Background(BrightBlue));

            l.Layer().Element(x =>
                x.TranslateX(owX).TranslateY(owTop)
                 .Width(owW).Height(owH)
                 .Rotate(-5)
                 .Background(Colors.White));

            l.Layer().PaddingTop(24f * S).AlignCenter().Column(text =>
            {
                text.Item().AlignCenter().Element(c => RenderLogo(c, data));

                text.Item().AlignCenter().PaddingTop(4f * S)
                    .Text((data.OrgName ?? "Company Name").ToUpperInvariant())
                    .FontSize(8f * S)
                    .FontColor(Colors.White)
                    .LetterSpacing(2.2f * S);
            });
        });
    }

    private static void RenderLogo(IContainer container, EmployeeIdCardData data)
    {
        if (data.OrgLogoBytes is { Length: > 0 })
            container.Width(60f * S).Height(28f * S).Image(data.OrgLogoBytes).FitArea();
    }

    private static void FieldBlock(ColumnDescriptor col, string label, string value, string borderColor)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(6f * S)
                .Background(borderColor)
                .CornerRadius(3f * S);

            row.RelativeItem().PaddingLeft(10f * S).Column(stack =>
            {
                stack.Item()
                    .Text(label.ToUpperInvariant())
                    .FontSize(9f * S)
                    .Bold()
                    .FontColor(DarkText);
                stack.Item().PaddingTop(2f * S)
                    .Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                    .FontSize(10f * S)
                    .FontColor(MutedText);
            });
        });
    }

    // ---------------- BACK: Emergency contact, Address, QR, Signature (match modal) ----------------

    private static void ComposeBack(IContainer container, EmployeeIdCardData data)
    {
        var topBarH = 40f * S;
        var padX = 24f * S;
        var padY = 12f * S;
        var footerH = 120f * S;
        var qrBox = 128f * S;
        var sigH = 48f * S;
        var sigW = 80f * S;

        container.Column(col =>
        {
            col.Item().Height(topBarH).Background(Navy);

            col.Item().PaddingHorizontal(padX).PaddingVertical(padY).Column(c =>
            {
                c.Item().Text("Emergency Contact")
                    .FontSize(10f * S)
                    .Bold()
                    .FontColor(Navy)
                    .LetterSpacing(1.2f * S);

                c.Item().PaddingTop(4f * S).Column(ec =>
                {
                    BackFieldRow(ec, "Name", data.EmergencyContactName);
                    BackFieldRow(ec, "Phone", data.EmergencyContactPhone);
                    BackFieldRow(ec, "Relation", data.EmergencyContactRelation);
                });

                c.Item().PaddingTop(12f * S).Text("Address")
                    .FontSize(10f * S)
                    .Bold()
                    .FontColor(Navy)
                    .LetterSpacing(1.2f * S);
                c.Item().PaddingTop(2f * S)
                    .Text(string.IsNullOrWhiteSpace(data.Address) ? "—" : data.Address)
                    .FontSize(8f * S)
                    .FontColor(MutedText);

                c.Item().PaddingTop(12f * S).AlignCenter()
                    .Width(qrBox).Height(qrBox)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.White)
                    .CornerRadius(10f * S)
                    .Padding(6f * S)
                    .Element(qr =>
                    {
                        if (data.QrBytes is { Length: > 0 })
                            qr.Image(data.QrBytes).FitArea();
                        else
                            qr.AlignCenter().AlignMiddle()
                                .Text("QR")
                                .FontSize(10f * S)
                                .FontColor(MutedText);
                    });

                c.Item().PaddingTop(10f * S).AlignCenter().Column(sig =>
                {
                    sig.Item().AlignCenter().Text("Signature").FontSize(8f * S).Bold().FontColor(Navy);
                    var sigBox = sig.Item().AlignCenter().PaddingTop(4f * S).Height(sigH).Width(sigW)
                        .Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(4f * S);
                    if (data.SignatureBytes is { Length: > 0 })
                        sigBox.Image(data.SignatureBytes).FitArea();
                });
            });

            col.Item().Height(footerH).Element(GeoFooter);
        });
    }

    private static void BackFieldRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(42f * S).Text(label.ToUpperInvariant()).FontSize(8f * S).Bold().FontColor(DarkText);
            r.ConstantItem(6f * S).Text(":").FontSize(8f * S).Bold().FontColor(Navy);
            r.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(8f * S).FontColor(MutedText);
        });
    }

    private static void GeoFooter(IContainer container)
    {
        var footerH = 120f * S;

        var f1H = 100f * S;
        var f1W = CardW * 1.20f;
        var f1X = -0.10f * CardW;
        var f1Top = (-30f * S);

        var f2H = 60f * S;
        var f2W = CardW * 1.20f;
        var f2X = -0.10f * CardW;
        var f2Top = (-15f * S);

        container.Background(Navy).Layers(l =>
        {
            l.PrimaryLayer().Element(_ => { });

            l.Layer().Element(x =>
                x.TranslateX(f1X).TranslateY(f1Top)
                 .Width(f1W).Height(f1H)
                 .Rotate(5)
                 .Background(MidBlue));

            l.Layer().Element(x =>
                x.TranslateX(f2X).TranslateY(f2Top)
                 .Width(f2W).Height(f2H)
                 .Rotate(-8)
                 .Background(BrightBlue));

            l.Layer().AlignCenter().AlignMiddle().Column(text =>
            {
                text.Item().AlignCenter()
                    .Text("COMPANY NAME")
                    .FontSize(10f * S)
                    .Bold()
                    .FontColor(Colors.White)
                    .LetterSpacing(1.6f * S);

                text.Item().AlignCenter()
                    .Text("TAGLINE GOES HERE")
                    .FontSize(7f * S)
                    .FontColor(Colors.White)
                    .LetterSpacing(1.8f * S);
            });
        });
    }

    public sealed class EmployeeIdCardData
    {
        public string? OrgName { get; set; }
        public byte[]? OrgLogoBytes { get; set; }
        public string FullName { get; set; } = "";
        public string EmpNumber { get; set; } = "";
        public string Role { get; set; } = "";
        public string Email { get; set; } = "—";
        public string PhoneFormatted { get; set; } = "—";
        public string EmergencyContactName { get; set; } = "—";
        public string EmergencyContactPhone { get; set; } = "—";
        public string EmergencyContactRelation { get; set; } = "—";
        public string Address { get; set; } = "—";
        public byte[]? PhotoBytes { get; set; }
        public byte[]? QrBytes { get; set; }
        public byte[]? SignatureBytes { get; set; }
    }

    public sealed record CardImagePair(byte[] Front, byte[] Back);
}
