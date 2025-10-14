using Core.Services.IServices;
using Data.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Core.Services
{
    public class PdfGeneratorService : IPdfGeneratorService
    {
        private readonly ILogger<PdfGeneratorService> _logger;

        static PdfGeneratorService()
        {
            // Configure QuestPDF license and settings (Community license)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public PdfGeneratorService(ILogger<PdfGeneratorService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Order order, List<OrderItem> orderItems)
        {
            try
            {
                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Text("INVOICE")
                                .FontColor(Colors.Blue.Medium)
                                .SemiBold().FontSize(22);

                            row.ConstantItem(180).Column(col =>
                            {
                                col.Item().Text($"Invoice No: INV-{invoice.Id.ToString().PadLeft(6, '0')}");
                                col.Item().Text($"Date: {invoice.InvoiceDate:yyyy-MM-dd}");
                                col.Item().Text($"Order No: ORD-{order.Id.ToString().PadLeft(6, '0')}");
                            });
                        });

                        page.Content().Column(col =>
                        {
                            col.Spacing(12);

                            // Customer info
                            col.Item().Text("Customer Information").Bold();
                            col.Item().Text(text =>
                            {
                                text.Line(order.CustomerName);
                                text.Line($"Contact: {order.CustomerContact}");
                                text.Line($"Address: {order.CustomerAddress}");
                            });

                            // Items table
                            col.Item().Text("Order Items").Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(6);   // Item
                                    columns.RelativeColumn(2);   // Qty
                                    columns.RelativeColumn(3);   // Price/Sqm
                                    columns.RelativeColumn(3);   // Total
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellHeaderStyle).Text("Item");
                                    header.Cell().Element(CellHeaderStyle).Text("Quantity");
                                    header.Cell().Element(CellHeaderStyle).AlignRight().Text("Price/Sqm");
                                    header.Cell().Element(CellHeaderStyle).AlignRight().Text("Total");

                                    static IContainer CellHeaderStyle(IContainer container) =>
                                        container.DefaultTextStyle(x => x.SemiBold())
                                                 .PaddingVertical(6)
                                                 .BorderBottom(1)
                                                 .BorderColor(Colors.Grey.Lighten2);
                                });

                                foreach (var item in orderItems)
                                {
                                    table.Cell().Element(CellBodyStyle).Text(item.Product?.Name ?? "N/A");
                                    table.Cell().Element(CellBodyStyle).Text(item.Quantity.ToString());
                                    table.Cell().Element(CellBodyStyle).AlignRight().Text($"${item.PricePerSqm:F2}");
                                    table.Cell().Element(CellBodyStyle).AlignRight().Text($"${item.TotalPrice:F2}");
                                }

                                static IContainer CellBodyStyle(IContainer container) =>
                                    container.PaddingVertical(6)
                                             .BorderBottom(1)
                                             .BorderColor(Colors.Grey.Lighten4);
                            });

                            // Total
                            col.Item().AlignRight().Text($"Total Amount: ${invoice.TotalAmount:F2}")
                                .FontSize(13).SemiBold();
                        });

                        page.Footer().AlignCenter().Text(t =>
                        {
                            t.Span("Generated ").FontColor(Colors.Grey.Darken1).FontSize(9);
                            t.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontColor(Colors.Grey.Darken1).FontSize(9);
                        });
                    });
                }).GeneratePdf();

                return await Task.FromResult(pdf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF");
                throw;
            }
        }

        public async Task<byte[]> GenerateReturnRequestPdfAsync(ReturnRequest returnRequest, Order order, List<ReturnItem> returnItems)
        {
            try
            {
                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Text("RETURN REQUEST")
                                .FontColor(Colors.Orange.Medium)
                                .SemiBold().FontSize(22);

                            row.ConstantItem(220).Column(col =>
                            {
                                col.Item().Text($"Return No: RET-{returnRequest.Id.ToString().PadLeft(6, '0')}");
                                col.Item().Text($"Date: {returnRequest.RequestDate:yyyy-MM-dd}");
                                col.Item().Text($"Order No: ORD-{order.Id.ToString().PadLeft(6, '0')}");
                                col.Item().Text($"Status: {returnRequest.Status}");
                            });
                        });

                        page.Content().Column(col =>
                        {
                            col.Spacing(12);

                            // Customer info
                            col.Item().Text("Customer Information").Bold();
                            col.Item().Text(text =>
                            {
                                // Use Line for each field to ensure they appear one under another
                                text.Line(order.CustomerName);
                                text.Line($"Contact: {order.CustomerContact}");
                                text.Line($"Address: {order.CustomerAddress}");
                            });

                            // Reason
                            col.Item().Text("Return Reason").Bold();
                            col.Item().Text(text =>
                            {
                                text.Line(returnRequest.ReturnReason);
                                if (!string.IsNullOrWhiteSpace(returnRequest.Description))
                                {
                                    text.Line(""); // small separator line
                                    text.Span("Description: ").SemiBold();
                                    text.Span(returnRequest.Description);
                                }
                            });

                            // Items table
                            col.Item().Text("Return Items").Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(6); // Item
                                    columns.RelativeColumn(2); // Qty
                                    columns.RelativeColumn(6); // Reason
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellHeaderStyle).Text("Item");
                                    header.Cell().Element(CellHeaderStyle).Text("Quantity");
                                    header.Cell().Element(CellHeaderStyle).Text("Reason");

                                    static IContainer CellHeaderStyle(IContainer container) =>
                                        container.DefaultTextStyle(x => x.SemiBold())
                                                 .PaddingVertical(6)
                                                 .BorderBottom(1)
                                                 .BorderColor(Colors.Grey.Lighten2);
                                });

                                foreach (var item in returnItems)
                                {
                                    table.Cell().Element(CellBodyStyle).Text(item.Product?.Name ?? "N/A");
                                    table.Cell().Element(CellBodyStyle).Text(item.Quantity.ToString());
                                    table.Cell().Element(CellBodyStyle).Text(item.Reason);
                                }

                                static IContainer CellBodyStyle(IContainer container) =>
                                    container.PaddingVertical(6)
                                             .BorderBottom(1)
                                             .BorderColor(Colors.Grey.Lighten4);
                            });

                            if (!string.IsNullOrWhiteSpace(returnRequest.AdminNotes))
                            {
                                col.Item().Text("Admin Notes").Bold();
                                col.Item().Text(returnRequest.AdminNotes);
                            }
                        });

                        page.Footer().AlignCenter().Text(t =>
                        {
                            t.Span("Generated ").FontColor(Colors.Grey.Darken1).FontSize(9);
                            t.Span($"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontColor(Colors.Grey.Darken1).FontSize(9);
                        });
                    });
                }).GeneratePdf();

                return await Task.FromResult(pdf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating return request PDF");
                throw;
            }
        }
    }
}
