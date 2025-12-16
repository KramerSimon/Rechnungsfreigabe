import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PdfUploadDashboardComponent } from './pdf-upload-dashboard.component';

describe('PdfUploadDashboardComponent', () => {
  let component: PdfUploadDashboardComponent;
  let fixture: ComponentFixture<PdfUploadDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PdfUploadDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PdfUploadDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
