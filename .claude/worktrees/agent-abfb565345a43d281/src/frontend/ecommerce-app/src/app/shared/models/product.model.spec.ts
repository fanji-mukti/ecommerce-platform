import { describe, it, expect } from 'vitest';
import type { Product, PagedResult } from './product.model';

describe('Product model', () => {
  it('should have all required Product fields', () => {
    const product: Product = {
      id: '00000000-0000-0000-0000-000000000001',
      name: 'Test Widget',
      sku: 'TST-001',
      description: 'A test product',
      price: 9.99,
      stockQuantity: 100,
      category: 'Electronics',
      imageUrl: null,
    };

    expect(product.id).toBe('00000000-0000-0000-0000-000000000001');
    expect(product.name).toBe('Test Widget');
    expect(product.price).toBe(9.99);
    expect(product.imageUrl).toBeNull();
  });

  it('should support PagedResult generic wrapper', () => {
    const pagedResult: PagedResult<Product> = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 12,
    };

    expect(pagedResult.items).toHaveLength(0);
    expect(pagedResult.totalCount).toBe(0);
    expect(pagedResult.page).toBe(1);
    expect(pagedResult.pageSize).toBe(12);
  });
});
