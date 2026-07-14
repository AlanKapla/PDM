import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import MultiDocumentDropzone from './MultiDocumentDropzone';
import { renderWithChakra } from '../../test/render-with-chakra';

describe('MultiDocumentDropzone', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('render_bezPlikow_pokazujePustyStan', () => {
    const onChange = vi.fn();

    renderWithChakra(
      <MultiDocumentDropzone files={[]} onChange={onChange} />
    );

    expect(screen.getByText(/Przeciągnij pliki lub kliknij/i)).toBeInTheDocument();
    expect(screen.getByText(/JPG, PNG · łącznie maks. 50 MB/i)).toBeInTheDocument();
  });

  it('wyborPlikow_aktualizujeListe', () => {
    const onChange = vi.fn();
    const file = new File(['content'], 'invoice.jpg', { type: 'image/jpeg' });
    Object.defineProperty(file, 'size', { value: 1024 });

    renderWithChakra(
      <MultiDocumentDropzone files={[]} onChange={onChange} />
    );

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [file] } });

    expect(onChange).toHaveBeenCalledWith([file]);
  });

  it('przekroczenieLimitu_wywolujeOnSizeExceeded', () => {
    const onChange = vi.fn();
    const onSizeExceeded = vi.fn();
    const existingFile = new File(['a'], 'existing.jpg', { type: 'image/jpeg' });
    Object.defineProperty(existingFile, 'size', { value: 40 * 1024 * 1024 });

    const newFile = new File(['b'], 'new.jpg', { type: 'image/jpeg' });
    Object.defineProperty(newFile, 'size', { value: 15 * 1024 * 1024 });

    renderWithChakra(
      <MultiDocumentDropzone
        files={[existingFile]}
        onChange={onChange}
        onSizeExceeded={onSizeExceeded}
        maxTotalSizeMB={50}
      />
    );

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [newFile] } });

    expect(onSizeExceeded).toHaveBeenCalledWith(
      55 * 1024 * 1024,
      50 * 1024 * 1024
    );
    expect(onChange).not.toHaveBeenCalled();
  });

  it('usunieciePliku_aktualizujeListe', () => {
    const onChange = vi.fn();
    const file1 = new File(['a'], 'a.jpg', { type: 'image/jpeg' });
    const file2 = new File(['b'], 'b.jpg', { type: 'image/jpeg' });
    Object.defineProperty(file1, 'size', { value: 1024 });
    Object.defineProperty(file2, 'size', { value: 2048 });

    renderWithChakra(
      <MultiDocumentDropzone files={[file1, file2]} onChange={onChange} />
    );

    fireEvent.click(screen.getByRole('button', { name: /Usuń plik a.jpg/i }));

    expect(onChange).toHaveBeenCalledWith([file2]);
  });

  it('plikNieobrazkowy_jestFiltrowany', () => {
    const onChange = vi.fn();
    const pdfFile = new File(['pdf'], 'doc.pdf', { type: 'application/pdf' });
    Object.defineProperty(pdfFile, 'size', { value: 1024 });

    renderWithChakra(
      <MultiDocumentDropzone files={[]} onChange={onChange} />
    );

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [pdfFile] } });

    expect(onChange).toHaveBeenCalledWith([]);
  });
});
