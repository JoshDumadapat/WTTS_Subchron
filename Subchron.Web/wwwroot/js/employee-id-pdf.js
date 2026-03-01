/**
 * Employee ID card – single print (PDF) and batch print/export/PDF.
 * Used by Employee Management page. Call init() after DOM and page helpers (getSelectedEmpIds, openIdModal) exist.
 */
(function (global) {
    'use strict';

    var BATCH_PAGE_CSS = '.batch-page{page-break-after:always;}.batch-page:last-child{page-break-after:auto;} ' +
        '.batch-row{display:flex;gap:4mm;justify-content:center;margin-bottom:4mm;} ' +
        '.batch-card{width:65mm;height:100mm;border:1px solid #ccc;border-radius:8px;overflow:hidden;font-size:8px;flex-shrink:0;} ' +
        '.batch-card-header{background:#002d5b;color:#fff;text-align:center;padding:4px;font-weight:bold;} ' +
        '.batch-card-tagline{background:#002d5b;color:#fff;text-align:center;padding:2px;font-size:6px;} ' +
        '.batch-card-photo{width:40px;height:40px;margin:4px auto;border-radius:50%;overflow:hidden;} .batch-card-photo img{width:100%;height:100%;object-fit:cover;} ' +
        '.batch-card-name{text-align:center;font-weight:bold;margin:2px 0;} .batch-card-role{text-align:center;margin-bottom:4px;} ' +
        '.batch-card-line{margin:1px 4px;} .batch-card-footer{height:8px;background:#002d5b;} ' +
        '.batch-card-back .batch-card-header-sm{height:8px;background:#002d5b;} ' +
        '.batch-card-terms{font-size:7px;margin:4px 0;} .batch-card-ul{margin:2px 0;padding-left:12px;font-size:6px;} ' +
        '.batch-card-qr{text-align:center;margin:4px 0;} .batch-card-footer-sm{background:#002d5b;color:#fff;text-align:center;padding:4px;font-size:6px;}';

    function escapeScriptClose(html) {
        var scriptClose = '<' + '/script';
        return String(html).replace(new RegExp(scriptClose, 'gi'), '<\\/script');
    }

    function buildBatchIdCardHtml(row, side) {
        var name = row.getAttribute('data-name') || '—';
        var empNumber = row.getAttribute('data-emp-number') || '—';
        var role = row.getAttribute('data-role') || '—';
        var email = row.getAttribute('data-email') || '—';
        var phone = row.getAttribute('data-phone') || '—';
        var photoUrl = row.getAttribute('data-id-picture-url') || row.getAttribute('data-avatar-url') || '';
        var dateHired = row.getAttribute('data-date-hired') || '';
        var empId = row.getAttribute('data-emp-id');
        var joinStr = '—';
        var expireStr = '—';
        if (dateHired) {
            var d = new Date(dateHired);
            if (!isNaN(d.getTime())) {
                joinStr = (d.getMonth() + 1).toString().padStart(2, '0') + '-' + d.getDate().toString().padStart(2, '0') + '-' + d.getFullYear();
                var ed = new Date(d);
                ed.setFullYear(ed.getFullYear() + 2);
                expireStr = (ed.getMonth() + 1).toString().padStart(2, '0') + '-' + ed.getDate().toString().padStart(2, '0') + '-' + ed.getFullYear();
            }
        }
        var qrSrc = global.location.origin + global.location.pathname + '?handler=AttendanceQr&id=' + encodeURIComponent(empId);
        if (side === 'front') {
            return '<div class="batch-card batch-card-front"><div class="batch-card-header">COMPANY NAME</div><div class="batch-card-tagline">TAGLINE GOES HERE</div>' +
                '<div class="batch-card-photo"><img src="' + (photoUrl || '') + '" alt="" onerror="this.style.display=\'none\'"/></div>' +
                '<div class="batch-card-name">' + name + '</div><div class="batch-card-role">' + role + '</div>' +
                '<div class="batch-card-line">ID NO : ' + empNumber + '</div><div class="batch-card-line">EMAIL : ' + (email !== '—' ? email : '—') + '</div><div class="batch-card-line">PHONE : ' + (phone !== '—' ? '+63 ' + phone : '—') + '</div>' +
                '<div class="batch-card-footer"></div></div>';
        }
        return '<div class="batch-card batch-card-back"><div class="batch-card-header-sm"></div>' +
            '<h4 class="batch-card-terms">Terms &amp; Condition</h4><ul class="batch-card-ul"><li>This card is the property of the company.</li><li>Report lost or stolen cards to HR.</li></ul>' +
            '<div class="batch-card-line">JOIN DATE : ' + joinStr + '</div><div class="batch-card-line">EXPIRE DATE : ' + expireStr + '</div>' +
            '<div class="batch-card-qr"><img src="' + qrSrc + '" alt="QR" width="80" height="80"/></div>' +
            '<div class="batch-card-footer-sm">COMPANY NAME<br/>TAGLINE GOES HERE</div></div>';
    }

    function rowsFromIds(getSelectedEmpIds, doc) {
        var ids = getSelectedEmpIds();
        return ids.map(function (id) { return doc.querySelector('.employee-row[data-emp-id="' + id + '"]'); }).filter(Boolean);
    }

    function buildBatchPagesHtml(rows) {
        var allHtml = '';
        for (var p = 0; p < rows.length; p += 3) {
            var slice = rows.slice(p, p + 3);
            allHtml += '<div class="batch-page"><div class="batch-row">';
            slice.forEach(function (row) { allHtml += buildBatchIdCardHtml(row, 'front'); });
            allHtml += '</div><div class="batch-row">';
            slice.forEach(function (row) { allHtml += buildBatchIdCardHtml(row, 'back'); });
            allHtml += '</div></div>';
        }
        return allHtml;
    }

    function openPrintWindow(html, title) {
        var printWin = global.open('', '_blank');
        if (!printWin) return;
        printWin.document.write(html);
        printWin.document.close();
        printWin.focus();
        setTimeout(function () { printWin.print(); printWin.close(); }, 300);
    }

    function captureElement(el) {
        return new Promise(function (resolve, reject) {
            if (typeof global.html2canvas !== 'function') {
                reject(new Error('html2canvas not loaded'));
                return;
            }
            global.html2canvas(el, {
                useCORS: true,
                allowTaint: true,
                scale: 2,
                logging: false
            }).then(function (canvas) {
                try {
                    resolve(canvas.toDataURL('image/png'));
                } catch (e) {
                    reject(e);
                }
            }).catch(reject);
        });
    }

    function delay(ms) {
        return new Promise(function (resolve) { setTimeout(resolve, ms); });
    }

    function waitForImages(container) {
        if (!container) return Promise.resolve();
        var imgs = Array.from(container.querySelectorAll('img'));
        if (!imgs.length) return Promise.resolve();
        return Promise.all(imgs.map(function (img) {
            if (img.complete) return Promise.resolve();
            return new Promise(function (resolve) {
                var done = function () { resolve(); };
                img.onload = done;
                img.onerror = done;
            });
        }));
    }

    function prepareCaptureContainers(doc) {
        var front = doc.getElementById('idCardFront');
        var back = doc.getElementById('idCardBack');
        var frontCapture = doc.getElementById('idCardFrontCapture');
        var backCapture = doc.getElementById('idCardBackCapture');
        if (!front || !back || !frontCapture || !backCapture) return false;

        frontCapture.innerHTML = '';
        backCapture.innerHTML = '';

        var frontClone = front.cloneNode(true);
        frontCapture.appendChild(frontClone);

        var backClone = back.cloneNode(true);
        backClone.classList.remove('id-card-back');
        backClone.style.transform = '';
        backClone.style.backfaceVisibility = '';
        backClone.style.background = '#f8fafc';
        backCapture.appendChild(backClone);

        return true;
    }

    async function captureCardImagesForRow(row, doc) {
        if (typeof global.populateIdCardFromRow !== 'function')
            throw new Error('ID card template is unavailable.');

        var data = global.populateIdCardFromRow(row);
        if (!data)
            return { skipped: true };
        if (!data.hasPhoto)
            return { skipped: true, name: data.name || 'Employee' };

        await delay(30);
        if (!prepareCaptureContainers(doc))
            throw new Error('Unable to prepare card capture.');

        var frontCapture = doc.getElementById('idCardFrontCapture');
        var backCapture = doc.getElementById('idCardBackCapture');

        await delay(10);
        await waitForImages(frontCapture);
        await waitForImages(backCapture);

        var front = await captureElement(frontCapture);
        var back = await captureElement(backCapture);

        return {
            skipped: false,
            empId: data.empId,
            name: data.name || 'Employee',
            frontImageBase64: front,
            backImageBase64: back
        };
    }

    async function captureCardsSequential(rows, doc, updateStatus) {
        var summary = { cards: [], skipped: [] };
        for (var i = 0; i < rows.length; i++) {
            if (typeof updateStatus === 'function') updateStatus(i, rows.length);
            var row = rows[i];
            if (!row) continue;
            var result = await captureCardImagesForRow(row, doc);
            if (!result) continue;
            if (result.skipped) {
                if (result.name) summary.skipped.push(result.name);
                continue;
            }
            summary.cards.push({
                id: result.empId,
                frontImageBase64: result.frontImageBase64,
                backImageBase64: result.backImageBase64
            });
        }
        return summary;
    }

    function submitBatchPdf(cards, doc) {
        var path = global.location.pathname || '/App/Employee/EmployeeManagement';
        var formData = new FormData();
        formData.append('cardsJson', JSON.stringify(cards));
        var token = doc.querySelector('input[name="__RequestVerificationToken"]');
        if (token && token.value) formData.append('__RequestVerificationToken', token.value);
        return global.fetch(path + '?handler=DownloadBatchIdsFromImages', {
            method: 'POST',
            body: formData
        }).then(function (resp) {
            if (!resp.ok) throw new Error('Unable to generate batch PDF.');
            return resp.blob();
        }).then(function (blob) {
            var url = URL.createObjectURL(blob);
            var a = doc.createElement('a');
            a.href = url;
            a.download = 'employee-ids-batch.pdf';
            a.click();
            URL.revokeObjectURL(url);
        });
    }

    function initSingleIdPrint(doc) {
        var btn = doc.getElementById('idCardPrintBtn');
        if (!btn) return;
        btn.addEventListener('click', function () {
            var noId = doc.getElementById('idCardNoIdMessage');
            if (noId && !noId.classList.contains('hidden')) return;
            var empId = btn.getAttribute('data-emp-id');
            if (!empId) return;

            var useCanvas = typeof global.html2canvas === 'function' && prepareCaptureContainers(doc);

            if (!useCanvas) {
                var path = global.location.pathname || '/App/Employee/EmployeeManagement';
                var url = path + (path.indexOf('?') >= 0 ? '&' : '?') + 'handler=DownloadIdPdf&id=' + encodeURIComponent(empId);
                global.location.href = url;
                return;
            }

            var originalText = btn.textContent;
            btn.disabled = true;
            btn.textContent = 'Generating…';

            var frontCapture = doc.getElementById('idCardFrontCapture');
            var backCapture = doc.getElementById('idCardBackCapture');

            captureElement(frontCapture)
                .then(function (frontDataUrl) {
                    return captureElement(backCapture).then(function (backDataUrl) {
                        return { front: frontDataUrl, back: backDataUrl };
                    });
                })
                .then(function (images) {
                    var path = global.location.pathname || '/App/Employee/EmployeeManagement';
                    var formData = new FormData();
                    formData.append('id', empId);
                    formData.append('frontImageBase64', images.front);
                    formData.append('backImageBase64', images.back);
                    var token = doc.querySelector('input[name="__RequestVerificationToken"]');
                    if (token && token.value) formData.append('__RequestVerificationToken', token.value);

                    return global.fetch(path + '?handler=DownloadIdPdfFromImages', {
                        method: 'POST',
                        body: formData
                    }).then(function (resp) {
                        if (!resp.ok) throw new Error('PDF generation failed');
                        return resp.blob();
                    }).then(function (blob) {
                        var url = URL.createObjectURL(blob);
                        var a = doc.createElement('a');
                        a.href = url;
                        a.download = 'employee-id-' + empId + '.pdf';
                        a.click();
                        URL.revokeObjectURL(url);
                    });
                })
                .catch(function () {
                    var path = global.location.pathname || '/App/Employee/EmployeeManagement';
                    global.location.href = path + (path.indexOf('?') >= 0 ? '&' : '?') + 'handler=DownloadIdPdf&id=' + encodeURIComponent(empId);
                })
                .then(function () {
                    btn.disabled = false;
                    btn.textContent = originalText;
                });
        });
    }

    function initBatchActions(doc, getSelectedEmpIds, openIdModal) {
        var getIds = getSelectedEmpIds;
        var openModal = openIdModal;

        doc.getElementById('batchPrintIds')?.addEventListener('click', function () {
            var ids = getIds();
            if (ids.length === 0) return;
            if (ids.length === 1) { openModal(ids[0]); return; }
            var rows = rowsFromIds(getIds, doc);
            var allHtml = buildBatchPagesHtml(rows);
            var html = '<!DOCTYPE html><html><head><meta charset="utf-8"><title>ID Cards – Print</title><style>' + BATCH_PAGE_CSS + '</style></' + 'head><body style="margin:8px;">' + allHtml + '</body></' + 'html>';
            openPrintWindow(html, 'ID Cards – Print');
        });

        var batchDownloadBtn = doc.getElementById('batchDownloadPdf');
        if (batchDownloadBtn) {
            batchDownloadBtn.addEventListener('click', async function () {
                var ids = getIds();
                if (ids.length === 0) return;
                var rows = rowsFromIds(getIds, doc).filter(Boolean);
                if (!rows.length) return;

                var btn = batchDownloadBtn;
                var originalHtml = btn.innerHTML;
                btn.disabled = true;
                btn.textContent = 'Preparing…';

                try {
                    var summary = await captureCardsSequential(rows, doc, function (index, total) {
                        btn.textContent = 'Preparing ' + (index + 1) + '/' + total;
                    });
                    if (!summary.cards.length)
                        throw new Error('No IDs with photos selected.');
                    await submitBatchPdf(summary.cards, doc);
                    if (typeof global.showToast === 'function')
                        global.showToast('Batch ID PDF downloaded.', true);
                    if (summary.skipped.length && typeof global.showToast === 'function') {
                        global.showToast(summary.skipped.length + ' skipped (missing ID photo).', false);
                    }
                }
                catch (err) {
                    var message = err && err.message ? err.message : 'Failed to generate batch PDF.';
                    if (typeof global.showToast === 'function') global.showToast(message, false);
                    else alert(message);
                }
                finally {
                    btn.disabled = false;
                    btn.innerHTML = originalHtml;
                }
            });
        }
    }

    function init(deps) {
        var doc = deps && deps.document ? deps.document : document;
        initSingleIdPrint(doc);
        if (deps && typeof deps.getSelectedEmpIds === 'function' && typeof deps.openIdModal === 'function') {
            initBatchActions(doc, deps.getSelectedEmpIds, deps.openIdModal);
        }
    }

    global.EmployeeIdPdf = {
        init: init,
        buildBatchIdCardHtml: buildBatchIdCardHtml,
        BATCH_PAGE_CSS: BATCH_PAGE_CSS
    };
})(typeof window !== 'undefined' ? window : this);
