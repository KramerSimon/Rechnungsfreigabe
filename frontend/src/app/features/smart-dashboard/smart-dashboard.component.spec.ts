import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SmartDashboardComponent } from './smart-dashboard.component';

describe('SmartDashboardComponent', () => {
  let component: SmartDashboardComponent;
  let fixture: ComponentFixture<SmartDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SmartDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SmartDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
