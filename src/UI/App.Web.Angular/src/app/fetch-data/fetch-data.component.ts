import { Component, Inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public logEntries: string[] = [];

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) {
  }

  ngOnInit(): void {
    // this.http.get<dataResponse>(this.baseUrl + 'mainservice')
    //   .subscribe(result => {
    //       console.dir(result);
    //       this.messages = result.logEntries;
    //     }, error =>
    //       console.error(error)
    //   );

    this.http.post<DataResponse>('http://localhost:31381/api/mainservice', null)
      .subscribe(result => {
          console.dir(result);
          this.logEntries = result.logEntries;
        }, (error: ErrorType) => {
          if (error.error.detail !== undefined) {
            const result: DataResponse = JSON.parse(error.error.detail);
            console.error(result);
            this.logEntries = result.logEntries;
          }
        }
      );
  }
}

interface DataResponse {
  logEntries: string[]
  logEvents: string[]
}

interface ErrorType {
  error: ErrorDetail
}

interface ErrorDetail {
  detail: string,
  instance: string
}
