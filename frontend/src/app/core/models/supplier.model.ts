export interface Supplier {
  id: number;
  name: string;
  legal_name?: string;
  tax_number?: string;
  vat_number?: string;
  address_line1?: string;
  address_line2?: string;
  postal_code?: string;
  city?: string;
  country?: string;
  email?: string;
  phone?: string;
  bank_name?: string;
  iban?: string;
  bic?: string;
  payment_terms_days?: number;
  created_at?: string;
  updated_at?: string;
}
