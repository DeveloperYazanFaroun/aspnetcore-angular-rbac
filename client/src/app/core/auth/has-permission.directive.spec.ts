import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, map } from 'rxjs';
import { AuthService } from './auth.service';
import { HasPermissionDirective } from './has-permission.directive';

@Component({
  imports: [HasPermissionDirective],
  template: `
    <button *appHasPermission="'invoices.approve'" id="approve">Approve</button>
    <button *appHasPermission="['invoices.view', 'invoices.export']" id="export">Export</button>
  `,
})
class HostComponent {}

describe('HasPermissionDirective', () => {
  const granted$ = new BehaviorSubject<ReadonlySet<string>>(new Set());

  beforeEach(() => {
    granted$.next(new Set());
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            hasAll$: (...required: string[]) => granted$.pipe(map((granted) => required.every((p) => granted.has(p)))),
          },
        },
      ],
    });
  });

  function render() {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('hides the element when the permission is missing', () => {
    const fixture = render();
    expect(fixture.nativeElement.querySelector('#approve')).toBeNull();
  });

  it('shows the element when the permission is granted', () => {
    granted$.next(new Set(['invoices.approve']));
    const fixture = render();
    expect(fixture.nativeElement.querySelector('#approve')).not.toBeNull();
  });

  it('requires every permission in a list', () => {
    granted$.next(new Set(['invoices.view']));
    const fixture = render();
    expect(fixture.nativeElement.querySelector('#export')).toBeNull();

    granted$.next(new Set(['invoices.view', 'invoices.export']));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('#export')).not.toBeNull();
  });

  it('reacts when permissions change at runtime', () => {
    granted$.next(new Set(['invoices.approve']));
    const fixture = render();

    granted$.next(new Set());
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('#approve')).toBeNull();
  });
});
