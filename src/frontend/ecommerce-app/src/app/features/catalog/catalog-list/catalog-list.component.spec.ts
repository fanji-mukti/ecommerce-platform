import { describe, it, expect } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { CatalogListComponent } from './catalog-list.component';

describe('CatalogListComponent', () => {
  it('renders Browse Products heading', async () => {
    await TestBed.configureTestingModule({
      imports: [CatalogListComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch()),
        provideRouter([]),
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(CatalogListComponent);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('h1')?.textContent?.trim()).toBe('Browse Products');
  });
});
